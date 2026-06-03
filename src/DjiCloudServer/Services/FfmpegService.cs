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

    /// <summary>Mata todos los procesos ffmpeg.exe huérfanos al arrancar el servidor.</summary>
    Task KillOrphansAsync();

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

    // ─ Estado legacy (un solo relay RTSP) ────────────────────────────────────
    private Process?      _legacyProcess;
    private readonly StringBuilder _legacyStderr = new();
    public bool    IsRunning     => _legacyProcess is { HasExited: false };
    public string? CurrentSource { get; private set; }
    public string  LastStderr    => _legacyStderr.ToString();
    public int     RtmpPort      => _streams.IsEmpty ? 0 : _streams.Values.First().Port;

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
        if (procs.Length == 0) return;

        foreach (var proc in procs)
        {
            try
            {
                logger.LogWarning("[ffmpeg] Huérfano eliminado al arrancar: PID {Pid}", proc.Id);
                proc.Kill(entireProcessTree: true);
            }
            catch { /* proceso ya muerto */ }
            finally { proc.Dispose(); }
        }
        await Task.Delay(600); // esperar a que los puertos se liberen
        logger.LogInformation("[ffmpeg] {Count} proceso(s) huérfano(s) eliminados.", procs.Length);
    }

    public string GetLastStderr(string? gatewaySn = null)
    {
        if (!string.IsNullOrWhiteSpace(gatewaySn) &&
            _streams.TryGetValue(gatewaySn, out var inst))
            return inst.Stderr.ToString();
        // Fallback: last dead instance stderr or legacy relay
        return _lastDeadStderr ?? _legacyStderr.ToString();
    }

    // Guarda el stderr del último proceso que murió para diagnóstico post-mortem
    private string? _lastDeadStderr;

    public async Task<StreamInfo?> StartRtmpListenerAsync(
        string gatewaySn, string localIp, int preferredPort, string hlsDir,
        string videoId = "", string aircraftSn = "")
    {
        // Si ya hay un stream para este SN, pararlo primero
        if (_streams.TryGetValue(gatewaySn, out var existing))
        {
            logger.LogInformation("[ffmpeg] Stream previo para {SN} detectado (puerto {Port}). Deteniéndolo.", gatewaySn, existing.Port);
            await StopInstanceAsync(gatewaySn, existing);
        }

        // Asignar puerto: protegido con semáforo para evitar race conditions
        int port;
        await _portLock.WaitAsync();
        try { port = FindFreePort(preferredPort); }
        finally { _portLock.Release(); }

        // Slug corto para el nombre de archivo HLS (evitar caracteres peligrosos)
        var slug    = SanitizeSlug(gatewaySn);
        var hlsFile = $"live-{slug}.m3u8";
        var pattern = Path.Combine(hlsDir, $"live-{slug}%03d.ts");
        var m3u8    = Path.Combine(hlsDir, hlsFile);

        Directory.CreateDirectory(hlsDir);
        foreach (var f in Directory.GetFiles(hlsDir, $"live-{slug}*")) File.Delete(f);

        var rtmpUrl = $"rtmp://{localIp}:{port}/live/drone";
        var ffmpegExe = ResolveFfmpegPath();

        var psi = new ProcessStartInfo(ffmpegExe)
        {
            RedirectStandardError  = true,
            RedirectStandardOutput = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        // ── Entrada RTMP listener ─────────────────────────────────────────────
        psi.ArgumentList.Add("-listen");  psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("-timeout"); psi.ArgumentList.Add("120000000");
        psi.ArgumentList.Add("-i");       psi.ArgumentList.Add($"rtmp://0.0.0.0:{port}/live/drone");

        // ── Mapeo ──────────────────────────────────────────────────────────────
        psi.ArgumentList.Add("-map"); psi.ArgumentList.Add("0:v:0");
        psi.ArgumentList.Add("-map"); psi.ArgumentList.Add("0:a:0?");

        // ── Códecs ─────────────────────────────────────────────────────────────
        psi.ArgumentList.Add("-c:v");          psi.ArgumentList.Add("libx264");
        psi.ArgumentList.Add("-preset");        psi.ArgumentList.Add("ultrafast");
        psi.ArgumentList.Add("-tune");          psi.ArgumentList.Add("zerolatency");
        psi.ArgumentList.Add("-crf");           psi.ArgumentList.Add("28");
        psi.ArgumentList.Add("-g");             psi.ArgumentList.Add("60");
        psi.ArgumentList.Add("-keyint_min");    psi.ArgumentList.Add("60");
        psi.ArgumentList.Add("-sc_threshold");  psi.ArgumentList.Add("0");
        psi.ArgumentList.Add("-c:a");           psi.ArgumentList.Add("aac");
        psi.ArgumentList.Add("-ar");            psi.ArgumentList.Add("44100");
        psi.ArgumentList.Add("-avoid_negative_ts"); psi.ArgumentList.Add("make_zero");
        psi.ArgumentList.Add("-fflags");        psi.ArgumentList.Add("+genpts+discardcorrupt");

        // ── HLS ────────────────────────────────────────────────────────────────
        psi.ArgumentList.Add("-f");              psi.ArgumentList.Add("hls");
        psi.ArgumentList.Add("-hls_time");       psi.ArgumentList.Add("2");
        psi.ArgumentList.Add("-hls_list_size");  psi.ArgumentList.Add("6");
        psi.ArgumentList.Add("-hls_flags");
        psi.ArgumentList.Add("delete_segments+append_list+omit_endlist+temp_file");
        psi.ArgumentList.Add("-hls_segment_filename"); psi.ArgumentList.Add(pattern);
        psi.ArgumentList.Add(m3u8);

        // Loggear el comando exacto para diagnóstico
        var cmdLine = $"{ffmpegExe} {string.Join(" ", psi.ArgumentList.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";
        logger.LogInformation("[ffmpeg:{SN}] Comando: {Cmd}", gatewaySn, cmdLine);

        try
        {
            var stderr = new StringBuilder();
            stderr.AppendLine($"CMD: {cmdLine}");
            var proc   = new Process { StartInfo = psi, EnableRaisingEvents = true };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                stderr.AppendLine(e.Data);
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("[ffmpeg:{SN}] {Line}", gatewaySn, e.Data);
            };
            proc.Exited += (_, _) =>
            {
                var tail = stderr.ToString();
                _lastDeadStderr = tail;
                logger.LogWarning("[ffmpeg:{SN}] Proceso terminó (port {Port}). Tail:\n{Stderr}",
                    gatewaySn, port, tail[^Math.Min(2000, tail.Length)..]);
                _streams.TryRemove(gatewaySn, out _);
            };

            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            // Esperar 2s: si ffmpeg muere de inmediato es un error de config/puerto
            await Task.Delay(2000);
            if (proc.HasExited)
            {
                // Dar tiempo al lector asíncrono para vaciar el buffer
                await Task.Delay(300);
                var errText = stderr.ToString();
                _lastDeadStderr = errText;
                logger.LogError("[ffmpeg:{SN}] Terminó inmediatamente (código {Code}).\n{Stderr}",
                    gatewaySn, proc.ExitCode, errText);
                proc.Dispose();
                return null;
            }

            var info     = new StreamInfo(gatewaySn, port, rtmpUrl, $"/hls/{hlsFile}",
                string.IsNullOrEmpty(aircraftSn) ? gatewaySn : aircraftSn,
                videoId);
            var instance = new FfmpegInstance(proc, port, info, stderr);
            _streams[gatewaySn] = instance;

            logger.LogInformation("[ffmpeg:{SN}] Escuchando RTMP en :{Port} → HLS {File}",
                gatewaySn, port, hlsFile);
            return info;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ffmpeg:{SN}] No se pudo iniciar en puerto {Port}.", gatewaySn, port);
            return null;
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

    public async Task<bool> StartAsync(string sourceUrl, string hlsDir)
    {
        Stop();
        _legacyStderr.Clear();

        Directory.CreateDirectory(hlsDir);
        foreach (var f in Directory.GetFiles(hlsDir, "live.*")) File.Delete(f);

        var m3u8    = Path.Combine(hlsDir, "live.m3u8");
        var pattern = Path.Combine(hlsDir, "live%03d.ts");
        var isRtsp  = sourceUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase);
        var isRtmp  = sourceUrl.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase);
        var ffmpegExe = ResolveFfmpegPath();

        var psi = new ProcessStartInfo(ffmpegExe)
        {
            RedirectStandardError  = true,
            RedirectStandardOutput = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        if (isRtsp)
        {
            psi.ArgumentList.Add("-rtsp_transport"); psi.ArgumentList.Add("tcp");
            psi.ArgumentList.Add("-rtsp_flags");     psi.ArgumentList.Add("prefer_tcp");
            psi.ArgumentList.Add("-timeout");        psi.ArgumentList.Add("5000000");
        }
        else if (isRtmp)
        {
            psi.ArgumentList.Add("-timeout"); psi.ArgumentList.Add("5000000");
        }

        psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(sourceUrl);
        psi.ArgumentList.Add("-map"); psi.ArgumentList.Add("0:v:0");
        psi.ArgumentList.Add("-map"); psi.ArgumentList.Add("0:a:0?");
        psi.ArgumentList.Add("-c:v");     psi.ArgumentList.Add("libx264");
        psi.ArgumentList.Add("-preset");  psi.ArgumentList.Add("ultrafast");
        psi.ArgumentList.Add("-tune");    psi.ArgumentList.Add("zerolatency");
        psi.ArgumentList.Add("-crf");     psi.ArgumentList.Add("28");
        psi.ArgumentList.Add("-g");       psi.ArgumentList.Add("60");
        psi.ArgumentList.Add("-keyint_min"); psi.ArgumentList.Add("60");
        psi.ArgumentList.Add("-sc_threshold"); psi.ArgumentList.Add("0");
        psi.ArgumentList.Add("-c:a");           psi.ArgumentList.Add("aac");
        psi.ArgumentList.Add("-ar");            psi.ArgumentList.Add("44100");
        psi.ArgumentList.Add("-avoid_negative_ts"); psi.ArgumentList.Add("make_zero");
        psi.ArgumentList.Add("-fflags");        psi.ArgumentList.Add("+genpts+discardcorrupt");
        psi.ArgumentList.Add("-f");             psi.ArgumentList.Add("hls");
        psi.ArgumentList.Add("-hls_time");      psi.ArgumentList.Add("2");
        psi.ArgumentList.Add("-hls_list_size"); psi.ArgumentList.Add("6");
        psi.ArgumentList.Add("-hls_flags");
        psi.ArgumentList.Add("delete_segments+append_list+omit_endlist+temp_file");
        psi.ArgumentList.Add("-hls_segment_filename");  psi.ArgumentList.Add(pattern);
        psi.ArgumentList.Add(m3u8);

        try
        {
            _legacyProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _legacyProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                _legacyStderr.AppendLine(e.Data);
            };
            _legacyProcess.Exited += (_, _) =>
            {
                if (CurrentSource is null) return;
                logger.LogWarning("[ffmpeg-legacy] Proceso terminó inesperadamente.");
                CurrentSource = null;
            };

            _legacyProcess.Start();
            _legacyProcess.BeginErrorReadLine();
            _legacyProcess.BeginOutputReadLine();

            await Task.Delay(4000);
            if (_legacyProcess.HasExited)
            {
                logger.LogError("[ffmpeg-legacy] Terminó con código {Code}.", _legacyProcess.ExitCode);
                CurrentSource = null;
                return false;
            }

            CurrentSource = sourceUrl;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ffmpeg-legacy] No se pudo iniciar.");
            CurrentSource = null;
            return false;
        }
    }

    public void Stop()
    {
        CurrentSource = null;
        if (_legacyProcess is { HasExited: false })
        {
            try { _legacyProcess.Kill(entireProcessTree: true); } catch { }
            _legacyProcess.WaitForExit(3000);
        }
        _legacyProcess?.Dispose();
        _legacyProcess = null;
    }

    public void Dispose()
    {
        Stop();
        StopAllAsync().GetAwaiter().GetResult();
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
