using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DjiCloudServer.Services;

// ── Interfaz pública ───────────────────────────────────────────────────────────

public interface IFfmpegService : IDisposable
{
    // ─ API multi-stream ──────────────────────────────────────────────────────
    /// <summary>Arranca un receptor RTMP para el gateway indicado. Devuelve la info del stream o null si falla.</summary>
    Task<StreamInfo?> StartRtmpListenerAsync(string gatewaySn, string localIp, int preferredPort, string hlsDir,
        string videoId = "", string aircraftSn = "");

    /// <summary>Detiene el stream del gateway indicado y libera su puerto.</summary>
    Task StopAsync(string gatewaySn);

    /// <summary>Detiene TODOS los streams activos.</summary>
    Task StopAllAsync();

    /// <summary>Estado de todos los streams activos.</summary>
    IReadOnlyList<StreamInfo> ActiveStreams { get; }

    /// <summary>Busca un stream por número de puerto RTMP.</summary>
    StreamInfo? GetByPort(int port);

    /// <summary>Busca un stream por SN de gateway.</summary>
    StreamInfo? GetBySn(string gatewaySn);

    /// <summary>Mata todos los procesos ffmpeg.exe y mediamtx.exe huérfanos al arrancar el servidor.</summary>
    Task KillOrphansAsync();

    /// <summary>Garantiza que el servidor de streaming MediaMTX esté en ejecución en segundo plano.</summary>
    Task EnsureMediaMtxRunningAsync();

    /// <summary>Devuelve la ruta del fichero mediamtx.log, o null si no se localiza el directorio.</summary>
    string? GetMediaMtxLogPath();

    /// <summary>Mata la instancia de MediaMTX en curso y la reinicia, para que aplique cambios en mediamtx.yml.</summary>
    Task RestartMediaMtxAsync();

    /// <summary>Devuelve el stderr del proceso ffmpeg asociado a un gateway SN, o el del relay legacy si sn es null.</summary>
    string GetLastStderr(string? gatewaySn = null);

    // ─ API legacy (relay RTSP/RTMP → HLS) ───────────────────────────────────
    Task<bool> StartAsync(string sourceUrl, string hlsDir);
    void Stop();
    bool    IsRunning     { get; }
    string? CurrentSource { get; }
    string  LastStderr    { get; }
    int     RtmpPort      { get; }
}

/// <summary>Información de un stream activo.</summary>
public sealed record StreamInfo(
    string  GatewaySn,
    int     Port,
    string  RtmpUrl,
    string  HlsPath,     // ruta relativa desde wwwroot, p.ej. /hls/live-SN.m3u8
    string  AircraftSn,
    string  VideoId      // video_id DJI completo, p.ej. {acSn}/{cam}/{idx}
);

// ── Implementación ─────────────────────────────────────────────────────────────

public sealed class FfmpegService(ILogger<FfmpegService> logger) : IFfmpegService
{
    // ─ Estado multi-stream ────────────────────────────────────────────────────
    private readonly ConcurrentDictionary<string, FfmpegInstance> _streams = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _portLock = new(1, 1);

    // ─ Estado MediaMTX y legacy (un solo relay RTSP) ──────────────────────────
    private Process?      _mediaMtxProcess;
    private readonly StringBuilder _legacyStderr = new();
    public bool    IsRunning     => false;
    public string? CurrentSource { get; private set; }
    public string  LastStderr    => _legacyStderr.ToString();
    public int     RtmpPort      => _streams.IsEmpty ? 0 : _streams.Values.First().Port;

    public string? GetMediaMtxLogPath()
    {
        var resolved = ResolveMediaMtx();
        return resolved.HasValue ? Path.Combine(resolved.Value.workingDir, "mediamtx.log") : null;
    }

    public async Task RestartMediaMtxAsync()
    {
        // Matar instancia actual (la interna y cualquier huérfano)
        if (_mediaMtxProcess != null && !_mediaMtxProcess.HasExited)
        {
            try { _mediaMtxProcess.Kill(entireProcessTree: true); } catch { }
            try { _mediaMtxProcess.Dispose(); } catch { }
            _mediaMtxProcess = null;
        }
        foreach (var proc in Process.GetProcessesByName("mediamtx"))
        {
            try { proc.Kill(entireProcessTree: true); proc.Dispose(); } catch { }
        }

        await Task.Delay(800); // esperar a que el proceso libere el puerto
        await EnsureMediaMtxRunningAsync();
    }

    private static (string exePath, string workingDir)? ResolveMediaMtx()
    {
        var baseDir = AppContext.BaseDirectory;
        var pathsToTry = new[]
        {
            Path.Combine(baseDir, "mediamate_server"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "mediamate_server"), // bin/Debug/net10.0 → project root
            Path.Combine(baseDir, "..", "..", "..", "..", "mediamate_server"),
            Path.Combine(baseDir, "..", "..", "..", "mediamate_server"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "mediamate_server")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "mediamate_server")),
        };

        foreach (var dir in pathsToTry)
        {
            var exe = Path.Combine(dir, "mediamtx.exe");
            if (File.Exists(exe))
            {
                return (exe, dir);
            }
        }

        if (File.Exists(Path.Combine(baseDir, "mediamtx.exe")))
            return (Path.Combine(baseDir, "mediamtx.exe"), baseDir);

        return null;
    }

    // ─ Helpers ────────────────────────────────────────────────────────────────

    private static string ResolveFfmpegPath()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe");
        if (File.Exists(bundled)) return bundled;
        var sibling = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(sibling)) return sibling;
        return "ffmpeg";
    }

    /// <summary>Devuelve el primer puerto ≥ <paramref name="start"/> que no esté siendo usado.</summary>
    private static int FindFreePort(int start = 1935)
    {
        for (var p = start; p < start + 20; p++)
        {
            if (!IsPortInUse(p)) return p;
        }
        throw new InvalidOperationException("No se encontró ningún puerto RTMP libre en el rango 1935-1954.");
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            s.Bind(new IPEndPoint(IPAddress.Any, port));
            return false;  // bind exitoso → nadie usa el puerto
        }
        catch (SocketException) { return true; }
    }

    private static async Task WaitForPortFreeAsync(int port, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (!IsPortInUse(port)) return;
            await Task.Delay(200);
        }
        // Si sigue ocupado tras el timeout, continuar igualmente
    }

    // ─ API multi-stream ───────────────────────────────────────────────────────

    public IReadOnlyList<StreamInfo> ActiveStreams =>
        _streams.Values.Where(i => i.IsAlive).Select(i => i.Info).ToList();

    public StreamInfo? GetByPort(int port) =>
        _streams.Values.FirstOrDefault(i => i.Port == port)?.Info;

    public StreamInfo? GetBySn(string gatewaySn) =>
        _streams.TryGetValue(gatewaySn, out var inst) && inst.IsAlive ? inst.Info : null;

    public async Task KillOrphansAsync()
    {
        var procs = Process.GetProcessesByName("ffmpeg");
        foreach (var proc in procs)
        {
            try
            {
                logger.LogWarning("[ffmpeg] Huérfano eliminado al arrancar: PID {Pid}", proc.Id);
                proc.Kill(entireProcessTree: true);
            }
            catch { }
            finally { proc.Dispose(); }
        }

        var mtxProcs = Process.GetProcessesByName("mediamtx");
        foreach (var proc in mtxProcs)
        {
            try
            {
                logger.LogWarning("[MediaMTX] Huérfano eliminado al arrancar: PID {Pid}", proc.Id);
                proc.Kill(entireProcessTree: true);
            }
            catch { }
            finally { proc.Dispose(); }
        }

        await Task.Delay(600); // esperar a que los puertos se liberen
        logger.LogInformation("[Streaming] Procesos huérfanos eliminados.");
    }

    public string GetLastStderr(string? gatewaySn = null)
    {
        if (!string.IsNullOrWhiteSpace(gatewaySn) &&
            _streams.TryGetValue(gatewaySn, out var inst))
            return inst.Stderr.ToString();
        // Fallback: legacy relay
        return _legacyStderr.ToString();
    }

    public async Task<StreamInfo?> StartRtmpListenerAsync(
        string gatewaySn, string localIp, int preferredPort, string hlsDir,
        string videoId = "", string aircraftSn = "")
    {
        logger.LogInformation("[Streaming] StartRtmpListenerAsync llamado para {GatewaySn} (FFmpeg desactivado incondicionalmente).", gatewaySn);
        await EnsureMediaMtxRunningAsync();
        return null;
    }

    public async Task EnsureMediaMtxRunningAsync()
    {
        var existing = Process.GetProcessesByName("mediamtx");
        if (existing.Length > 0)
        {
            logger.LogInformation("[MediaMTX] Ya se encuentra en ejecución ({Count} instancia(s)).", existing.Length);
            return;
        }

        var resolved = ResolveMediaMtx();
        if (resolved == null)
        {
            logger.LogError("[MediaMTX] ❌ No se encontró mediamtx.exe.");
            return;
        }

        var (exePath, workingDir) = resolved.Value;
        logger.LogInformation("[MediaMTX] Arrancando mediamtx.exe desde: {Path} con WorkingDir: {WDir}", exePath, workingDir);

        try
        {
            var psi = new ProcessStartInfo(exePath)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logger.LogDebug("[MediaMTX] {Log}", e.Data);
            };
            proc.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logger.LogWarning("[MediaMTX-Err] {Log}", e.Data);
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            _mediaMtxProcess = proc;
            logger.LogInformation("[MediaMTX] mediamtx.exe arrancado con PID {Pid}.", proc.Id);
            await Task.Delay(1000); // espera breve de inicialización
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MediaMTX] Error al arrancar mediamtx.exe");
        }
    }

    public async Task StopAsync(string gatewaySn)
    {
        if (_streams.TryRemove(gatewaySn, out var inst))
            await StopInstanceAsync(gatewaySn, inst);
    }

    public async Task StopAllAsync()
    {
        var keys = _streams.Keys.ToList();
        foreach (var k in keys)
        {
            if (_streams.TryRemove(k, out var inst))
                await StopInstanceAsync(k, inst);
        }
    }

    private async Task StopInstanceAsync(string gatewaySn, FfmpegInstance inst)
    {
        var port = inst.Port;
        try
        {
            if (!inst.Process.HasExited)
            {
                logger.LogInformation("[ffmpeg:{SN}] Kill proceso pid={Pid} port={Port}",
                    gatewaySn, inst.Process.Id, port);
                inst.Process.Kill(entireProcessTree: true);
            }

            // Cancelar los lectores asíncronos ANTES de WaitForExit.
            // En .NET 6+, WaitForExit(ms) con BeginErrorReadLine activo espera a que
            // el lector async complete — si no se cancela, produce un deadlock aunque
            // el proceso ya haya muerto.
            try { inst.Process.CancelErrorRead();  } catch { }
            try { inst.Process.CancelOutputRead(); } catch { }

            // Timeout estricto de 1 s en un hilo separado para no bloquear el async
            await Task.Run(() => inst.Process.WaitForExit(1000));
        }
        catch { /* proceso ya muerto o no arrancó */ }
        finally
        {
            try { inst.Process.Dispose(); } catch { }
        }

        logger.LogInformation("[ffmpeg:{SN}] Proceso detenido. Liberando puerto {Port}...", gatewaySn, port);

        // Esperar a que el SO libere el puerto (max 3 s); si no, continuar de todos modos
        await WaitForPortFreeAsync(port, timeoutMs: 3000);
        logger.LogInformation("[ffmpeg:{SN}] Puerto {Port} listo.", gatewaySn, port);
    }

    private static string SanitizeSlug(string sn) =>
        new string(sn.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray())[..Math.Min(20, sn.Length)];

    // ─ API legacy (relay RTSP/RTMP → HLS) ────────────────────────────────────

    public Task<bool> StartAsync(string sourceUrl, string hlsDir)
    {
        logger.LogWarning("[Streaming-legacy] StartAsync llamado para {Url} (FFmpeg desactivado incondicionalmente).", sourceUrl);
        return Task.FromResult(false);
    }

    public void Stop()
    {
        // Legacy Stop
    }

    public void Dispose()
    {
        Stop();
        // Espera acotada en lugar de GetAwaiter().GetResult() sin límite:
        // si un proceso ffmpeg se cuelga al cerrar, no retrasamos el shutdown
        // de la aplicación más de 5 segundos.
        try { StopAllAsync().Wait(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) { logger.LogWarning(ex, "[Streaming] Timeout/error deteniendo streams en Dispose"); }

        if (_mediaMtxProcess != null && !_mediaMtxProcess.HasExited)
        {
            try
            {
                logger.LogInformation("[MediaMTX] Deteniendo mediamtx.exe...");
                _mediaMtxProcess.Kill(entireProcessTree: true);
                _mediaMtxProcess.Dispose();
            }
            catch {}
            _mediaMtxProcess = null;
        }

        _portLock.Dispose();
    }

    // ─ Clase interna ──────────────────────────────────────────────────────────

    private sealed class FfmpegInstance(Process process, int port, StreamInfo info, StringBuilder stderr)
    {
        public Process       Process { get; } = process;
        public int           Port    { get; } = port;
        public StreamInfo    Info    { get; } = info;
        public StringBuilder Stderr  { get; } = stderr;
        public bool          IsAlive => !Process.HasExited;
    }
}
