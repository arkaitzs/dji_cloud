using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MQTTnet.Protocol;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using IOFile = System.IO.File;      // alias para evitar conflicto con ControllerBase.File()

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/stream")]
public class StreamController(
    IFfmpegService ffmpeg,
    IWebHostEnvironment env,
    IMqttService mqtt,
    IAdminDataService admin,
    IOptions<DjiCloudOptions> options,
    ILogger<StreamController> logger) : ControllerBase
{
    private const int BaseRtmpPort     = 1935;  // primer puerto del pool RTMP (FFmpeg)
    private const int MediaMtxRtmpPort = 1935;  // puerto RTMP de MediaMTX
    private const int MediaMtxPort     = 8889;  // puerto WebRTC de MediaMTX (WHEP)

    private static readonly ConcurrentDictionary<string, string> _activeVideoIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, bool> _activeProtocols = new(StringComparer.OrdinalIgnoreCase);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GetLocalIp()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = ua.Address.ToString();
                if (ip.StartsWith("169.254")) continue;
                return ip;
            }
        }
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 80);
            return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }

    private static List<object> GetAllLocalIps()
    {
        var list = new List<object>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = ua.Address.ToString();
                if (ip.StartsWith("169.254")) continue;
                list.Add(new { ip, name = ni.Name, description = ni.Description });
            }
        }
        return list;
    }

    /// <summary>
    /// Selecciona la IP del adaptador LAN real usando dos heurísticas:
    /// 1. La interfaz tiene un default gateway IPv4 configurado.
    /// 2. El último octeto de la IP NO es 1 (las .x.x.1 suelen ser adaptadores virtuales / hotspot).
    /// Devuelve null si no puede reducirse a un único candidato.
    /// </summary>
    private static string? GetPreferredLanIp()
    {
        var candidates = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;

            var props = ni.GetIPProperties();
            bool hasGateway = props.GatewayAddresses
                .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
            if (!hasGateway) continue;

            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = ua.Address.ToString();
                if (ip.StartsWith("169.254")) continue;
                if (ip.Split('.').LastOrDefault() == "1") continue;   // descartar adaptadores virtuales
                candidates.Add(ip);
            }
        }
        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static string? GetLocalIpOnSubnet(IPAddress clientIp)
    {
        var clientBytes = clientIp.GetAddressBytes();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var localBytes = ua.Address.GetAddressBytes();
                var maskBytes  = ua.IPv4Mask?.GetAddressBytes();
                if (maskBytes is null || maskBytes.Length != 4) continue;
                var sameSubnet = true;
                for (var i = 0; i < 4; i++)
                {
                    if ((clientBytes[i] & maskBytes[i]) != (localBytes[i] & maskBytes[i]))
                    { sameSubnet = false; break; }
                }
                if (sameSubnet) return ua.Address.ToString();
            }
        }
        return null;
    }

    private static bool IsLocalIpAddress(string ip)
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (ua.Address.ToString() == ip) return true;
            }
        }
        return false;
    }

    // ─── IPs locales ──────────────────────────────────────────────────────────

    [HttpGet("local-ip")]
    public IActionResult LocalIp() => Ok(new { ip = GetLocalIp() });

    [HttpGet("local-ips")]
    public IActionResult LocalIps() => Ok(GetAllLocalIps());

    // ─── Stream RTSP/RTMP manual (legacy) ────────────────────────────────────

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartStreamRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            return BadRequest(new { error = "Se requiere una URL de origen (rtsp:// o rtmp://)" });

        var hlsDir = Path.Combine(env.WebRootPath, "hls");
        var ok = await ffmpeg.StartAsync(req.Url, hlsDir);

        if (!ok)
            return StatusCode(500, new
            {
                error = "No se pudo iniciar ffmpeg. Comprueba que ffmpeg está en el PATH y que la URL es accesible."
            });

        return Ok(new { hlsUrl = "/hls/live.m3u8", source = req.Url });
    }

    // ─── Iniciar stream DJI Pilot 2 (RTMP listener, multi-stream) ────────────

    /// <summary>
    /// Inicia el stream del dron hacia el servidor de medios:
    ///   UseWebRtc = true  → MediaMTX (url_type=4, WHIP/WHEP, latencia &lt;500ms). FFmpeg no se usa.
    ///   UseWebRtc = false → FFmpeg receptor RTMP → HLS (flujo legacy).
    /// </summary>
    [HttpPost("start-live")]
    public async Task<IActionResult> StartLive([FromBody] StartLiveRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.GatewaySn))
            return BadRequest(new { error = "Se requiere el SN del gateway (mando o dock)." });

        // ── Asegurar que MediaMTX está corriendo en segundo plano ──
        await ffmpeg.EnsureMediaMtxRunningAsync();

        // ── Resolución de la IP (priorizando la IP fija configurada en appsettings.json) ──
        string detectedIp = "";
        var serverIp = options.Value.ServerIp;

        if (!string.IsNullOrWhiteSpace(serverIp) && IPAddress.TryParse(serverIp, out var parsedConfigIp) &&
            parsedConfigIp.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(parsedConfigIp) &&
            IsLocalIpAddress(serverIp))
        {
            detectedIp = serverIp;
            admin.AddLog("INFO", "LiveStream", $"IP del servidor configurada en appsettings: {detectedIp}");
        }
        else
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress;
            if (clientIp?.IsIPv4MappedToIPv6 == true) clientIp = clientIp.MapToIPv4();

            string? auto = null;
            if (clientIp is not null && !IPAddress.IsLoopback(clientIp) &&
                clientIp.AddressFamily == AddressFamily.InterNetwork)
            {
                auto = GetLocalIpOnSubnet(clientIp);
                if (auto is not null)
                    admin.AddLog("INFO", "LiveStream", $"IP detectada por subred del cliente: {auto}");
            }

            auto ??= GetPreferredLanIp();
            if (auto is not null)
                admin.AddLog("INFO", "LiveStream", $"IP seleccionada por heurística LAN: {auto}");

            detectedIp = auto ?? GetLocalIp();

            if (!IPAddress.TryParse(detectedIp, out var parsedIp) ||
                parsedIp.AddressFamily != AddressFamily.InterNetwork ||
                IPAddress.IsLoopback(parsedIp))
            {
                detectedIp = GetLocalIp();
            }
        }

        // ── Video ID y SN de aeronave ──────────────────────────────────────────
        var aircraftSn = admin.GetAircraftForGateway(req.GatewaySn) ?? req.GatewaySn;
        var cameraType = string.IsNullOrWhiteSpace(req.CameraType) ? "0-0-0" : req.CameraType;
        var videoIndex = string.IsNullOrWhiteSpace(req.VideoIndex) ? "normal-0" : req.VideoIndex;
        var videoId    = !string.IsNullOrWhiteSpace(req.VideoId)
            ? req.VideoId
            : $"{aircraftSn}/{cameraType}/{videoIndex}";

        // ── Configurar Protocolo según preferencia (WebRTC o RTMP) ──────────────────
        int urlType;
        string streamUrl;
        string whepUrl;

        if (req.UseWebRtc)
        {
            urlType = 4; // WebRTC (WHIP)
            int kestrelPort = Request.Host.Port ?? 5072;
            string scheme = Request.Scheme;
            streamUrl = $"{scheme}://{detectedIp}:{kestrelPort}/rtc/v1/whip?app=live&stream={aircraftSn}";
            whepUrl   = $"http://{detectedIp}:8889/{aircraftSn}/whep";
            logger.LogInformation("[MediaMTX WebRTC] WHIP={WhipUrl}  WHEP={WhepUrl}", streamUrl, whepUrl);
        }
        else
        {
            urlType = 1; // RTMP
            streamUrl = $"rtmp://{detectedIp}:1935/live/{aircraftSn}";
            whepUrl   = $"http://{detectedIp}:8889/live/{aircraftSn}/whep";
            logger.LogInformation("[MediaMTX RTMP] RTMP={RtmpUrl}  WHEP={WhepUrl}", streamUrl, whepUrl);
        }

        // ── Verificar MQTT ────────────────────────────────────────────────────
        if (!mqtt.IsConnected)
        {
            logger.LogError("[MQTT] ❌ Broker desconectado. Abortando live_start_push para {GatewaySn}.", req.GatewaySn);
            return StatusCode(503, new { error = "El broker MQTT no está conectado. Reinicia el servidor." });
        }

        // ── Construir y publicar live_start_push ──────────────────────────────
        var tid     = Guid.NewGuid().ToString();
        var bid     = Guid.NewGuid().ToString();
        var ts      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var quality = req.Quality >= 0 && req.Quality <= 4 ? req.Quality : 3;

        var payload = JsonSerializer.Serialize(new
        {
            tid,
            bid,
            timestamp = ts,
            gateway   = req.GatewaySn,
            method    = "live_start_push",
            data      = new
            {
                url_type      = urlType,
                url           = streamUrl,
                video_id      = videoId,
                video_quality = quality
            }
        }).Replace("\\u0026", "&");

        var topicGateway = $"thing/product/{req.GatewaySn}/services";

        logger.LogInformation(
            "[MQTT] >>> live_start_push  topic={Topic}  urlType={UrlType}  url={Url}  vidId={VideoId}",
            topicGateway, urlType, streamUrl, videoId);

        try
        {
            await mqtt.PublishAsync(topicGateway, payload, MqttQualityOfServiceLevel.AtLeastOnce);
            _activeVideoIds[req.GatewaySn] = videoId;
            _activeProtocols[req.GatewaySn] = req.UseWebRtc;
            
            admin.AddLog("INFO", "LiveStream",
                $"live_start_push → {req.GatewaySn} | url_type={urlType} | url={streamUrl} | video_id={videoId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MQTT] ❌ Excepción al publicar live_start_push → {GatewaySn}", req.GatewaySn);
            admin.AddLog("WARN", "LiveStream", $"live_start_push MQTT excepción: {ex.Message}");
            return StatusCode(503, new { error = $"Error MQTT al enviar live_start_push: {ex.Message}" });
        }

        return Ok(new
        {
            useWebRtc = req.UseWebRtc,
            whepUrl,        // URL WHEP para el browser (solo WebRTC)
            streamUrl,      // URL enviada al dron (WHIP)
            localIp = detectedIp,
            videoId,
            aircraftSn,
            mqttTopic  = topicGateway
        });
    }

    /// <summary>
    /// Proxy WHIP que intercepta la señalización SDP del dron en el puerto de Kestrel.
    /// Esto permite satisfacer las estrictas reglas de validación de URLs del firmware de DJI
    /// (que exigen la ruta /rtc/v1/whip/?app=live&stream={droneSn}) y a la vez redirigir la
    /// retransmisión al endpoint REST único de MediaMTX.
    /// </summary>
    [HttpPost("/rtc/v1/whip")]
    public async Task<IActionResult> WhipProxy([FromQuery] string stream)
    {
        if (string.IsNullOrWhiteSpace(stream))
            return BadRequest("Se requiere el parámetro 'stream' (SN del dron).");

        try
        {
            using var reader = new StreamReader(Request.Body);
            var sdpOffer = await reader.ReadToEndAsync();

            using var client = new HttpClient();
            using var content = new StringContent(sdpOffer, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/sdp");

            // Enviar el SDP a MediaMTX en su puerto local (8889) usando la ruta nativa
            var response = await client.PostAsync($"http://127.0.0.1:8889/{stream}/whip", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errText = await response.Content.ReadAsStringAsync();
                logger.LogError("[WHIP Proxy] Error de MediaMTX ({Status}): {Error}", response.StatusCode, errText);
                return StatusCode((int)response.StatusCode, errText);
            }

            var sdpAnswer = await response.Content.ReadAsStringAsync();
            return Content(sdpAnswer, "application/sdp");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[WHIP Proxy] Excepción al procesar el proxy WHIP para {Stream}", stream);
            return StatusCode(500, ex.Message);
        }
    }

    // ─── Detener stream específico (con live_stop_push correcto) ───────────────

    /// <summary>
    /// Detiene el stream del gateway indicado:
    /// 1. Obtiene el video_id almacenado en StreamInfo.
    /// 2. Mata su proceso ffmpeg y espera que libere el puerto.
    /// 3. Publica live_stop_push con el video_id correcto (especificación DJI).
    /// </summary>
    [HttpPost("stop-live/{gatewaySn}")]
    public async Task<IActionResult> StopLive(string gatewaySn)
    {
        // Recuperar video_id y protocolo de la caché o de ffmpeg info
        var info = ffmpeg.GetBySn(gatewaySn);
        _activeVideoIds.TryRemove(gatewaySn, out var videoId);
        _activeProtocols.TryRemove(gatewaySn, out var useWebRtc);

        videoId ??= info?.VideoId ?? "";

        // Detener ffmpeg (si se usó el flujo legacy)
        await ffmpeg.StopAsync(gatewaySn);

        // Publicar live_stop_push con el video_id del stream que acábamos de parar
        if (!string.IsNullOrWhiteSpace(gatewaySn))
        {
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    tid       = Guid.NewGuid().ToString(),
                    bid       = Guid.NewGuid().ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    gateway   = gatewaySn,
                    method    = "live_stop_push",
                    data      = string.IsNullOrEmpty(videoId)
                        ? (object)new { }
                        : new { video_id = videoId }
                });
                await mqtt.PublishAsync($"thing/product/{gatewaySn}/services", payload,
                    MqttQualityOfServiceLevel.AtLeastOnce);
                admin.AddLog("INFO", "LiveStream",
                    $"live_stop_push → {gatewaySn} | video_id: {videoId}");
            }
            catch { /* si falla el MQTT no bloqueamos la respuesta */ }
        }

        return Ok(new { stopped = true, gatewaySn, port = info?.Port, videoId });
    }

    // ─── Detener stream legacy (relay) ────────────────────────────────────────

    [HttpPost("stop")]
    public IActionResult Stop()
    {
        ffmpeg.Stop();
        return Ok(new { stopped = true });
    }

    // ─── Estado ───────────────────────────────────────────────────────────────

    /// <summary>Lista todos los streams activos.</summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        var streams = ffmpeg.ActiveStreams.Select(s => new
        {
            gatewaySn = s.GatewaySn,
            rtmpPort  = s.Port,
            rtmpUrl   = s.RtmpUrl,
            hlsUrl    = s.HlsPath,
            videoId   = s.VideoId
        }).ToList();

        return Ok(new
        {
            running       = ffmpeg.IsRunning || streams.Count > 0,
            activeStreams  = streams,
            // Legacy
            source        = ffmpeg.CurrentSource,
            hlsUrl        = ffmpeg.IsRunning ? "/hls/live.m3u8" : null,
            rtmpPort      = ffmpeg.RtmpPort
        });
    }

    /// <summary>Estado de un stream concreto por gateway SN.</summary>
    [HttpGet("status/{gatewaySn}")]
    public IActionResult StatusBySn(string gatewaySn)
    {
        var info = ffmpeg.GetBySn(gatewaySn);
        if (info is null) return Ok(new { running = false, gatewaySn });
        return Ok(new
        {
            running   = true,
            gatewaySn = info.GatewaySn,
            rtmpPort  = info.Port,
            hlsUrl    = info.HlsPath,
            rtmpUrl   = info.RtmpUrl
        });
    }

    [HttpGet("ready")]
    public IActionResult Ready()
    {
        // Comprueba cualquier m3u8 activo
        foreach (var s in ffmpeg.ActiveStreams)
        {
            var path = Path.Combine(env.WebRootPath, s.HlsPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (IOFile.Exists(path) && new FileInfo(path).Length > 0)
                return Ok(new { ready = true, running = true, hlsUrl = s.HlsPath, gatewaySn = s.GatewaySn });
        }
        // Fallback legacy
        var legacyPath = Path.Combine(env.WebRootPath, "hls", "live.m3u8");
        var exists     = IOFile.Exists(legacyPath);
        var size       = exists ? new FileInfo(legacyPath).Length : 0L;
        return Ok(new { ready = exists && size > 0, running = ffmpeg.IsRunning, size });
    }

    /// <summary>
    /// Devuelve las últimas N líneas del fichero mediamtx.log.
    /// ?lines=500 (por defecto). ?download=true fuerza la descarga del fichero completo.
    /// </summary>
    [HttpGet("mediamtx-log")]
    public IActionResult MediaMtxLog([FromQuery] int lines = 500, [FromQuery] bool download = false)
    {
        var logPath = ffmpeg.GetMediaMtxLogPath();

        if (logPath == null || !System.IO.File.Exists(logPath))
        {
            if (download) return NotFound(new { error = "mediamtx.log no encontrado." });
            return Ok(new { found = false, lines = Array.Empty<string>(), totalLines = 0 });
        }

        try
        {
            // FileShare.ReadWrite permite leer mientras mediamtx.exe mantiene el fichero abierto
            using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (download)
            {
                var ms = new MemoryStream();
                fs.CopyTo(ms);
                return File(ms.ToArray(), "text/plain; charset=utf-8", "mediamtx.log");
            }

            using var sr = new StreamReader(fs);
            var allLines = sr.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var tail     = allLines.Length > lines ? allLines[^lines..] : allLines;
            return Ok(new { found = true, lines = tail, totalLines = allLines.Length });
        }
        catch (Exception ex)
        {
            if (download) return StatusCode(500, new { error = ex.Message });
            return Ok(new { found = false, error = ex.Message, lines = Array.Empty<string>(), totalLines = 0 });
        }
    }

    /// <summary>Reinicia mediamtx.exe para que aplique cambios en mediamtx.yml (p.ej. nuevo logDestination).</summary>
    [HttpPost("restart-mediamtx")]
    public async Task<IActionResult> RestartMediaMtx()
    {
        await ffmpeg.RestartMediaMtxAsync();
        admin.AddLog("INFO", "MediaMTX", "mediamtx.exe reiniciado vía API");
        return Ok(new { restarted = true });
    }

    [HttpGet("log")]
    [HttpGet("log/{gatewaySn}")]
    public IActionResult Log(string? gatewaySn = null)
    {
        var raw   = ffmpeg.GetLastStderr(gatewaySn);
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return Ok(new { running = ffmpeg.IsRunning, lines });
    }

    [HttpGet("port-check")]
    [HttpGet("port-check/{gatewaySn}")]
    public IActionResult PortCheck(string? gatewaySn = null)
    {
        // Si se pasa un SN, verificar el puerto real de ese stream activo
        var info = string.IsNullOrWhiteSpace(gatewaySn) ? null : ffmpeg.GetBySn(gatewaySn);
        var portToCheck = info?.Port ?? BaseRtmpPort;

        bool portBound;
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(new IPEndPoint(IPAddress.Any, portToCheck));
            portBound = false;
        }
        catch (SocketException) { portBound = true; }

        return Ok(new
        {
            ffmpegRunning = ffmpeg.IsRunning || info != null,
            portBound,
            port         = portToCheck,
            activeStreams = ffmpeg.ActiveStreams.Count
        });
    }

    [HttpPost("open-firewall")]
    public IActionResult OpenFirewall()
    {
        try
        {
            var args = $"advfirewall firewall add rule name=\"DJI Cloud RTMP {BaseRtmpPort}-1954\" " +
                       $"dir=in action=allow protocol=TCP localport={BaseRtmpPort}-1954 enable=yes";
            var psi = new System.Diagnostics.ProcessStartInfo("netsh", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit(6000);
            var output = proc.StandardOutput.ReadToEnd().Trim();
            admin.AddLog("INFO", "Firewall", $"netsh puertos 1935-1954: {output}");
            return Ok(new { success = proc.ExitCode == 0, output });
        }
        catch (Exception ex) { return Ok(new { success = false, output = ex.Message }); }
    }

    [HttpGet("live-reply/{gatewaySn}")]
    public IActionResult LiveReply(string gatewaySn, [FromQuery] long since = 0)
    {
        var reply = admin.GetLastServicesReply(gatewaySn);
        if (reply is null) return Ok(new { received = false });
        // Solo aceptar respuestas live_start_push más recientes que el inicio del stream
        if (reply.Method != "live_start_push") return Ok(new { received = false });
        if (since > 0 && reply.Timestamp <= since) return Ok(new { received = false });
        return Ok(new { received = true, method = reply.Method, result = reply.Result, timestamp = reply.Timestamp });
    }

    [HttpPost("change-lens")]
    public async Task<IActionResult> ChangeLens([FromBody] ChangeLensRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.GatewaySn) || string.IsNullOrWhiteSpace(req.LensType))
            return BadRequest(new { error = "Se requiere GatewaySn y LensType (wide, zoom, thermal, normal)." });

        var info = ffmpeg.GetBySn(req.GatewaySn);
        var videoId = info?.VideoId;
        
        if (string.IsNullOrWhiteSpace(videoId))
        {
            var aircraftSn = admin.GetAircraftForGateway(req.GatewaySn) ?? req.GatewaySn;
            var cameraType = string.IsNullOrWhiteSpace(req.CameraType) ? "67-0-0" : req.CameraType;
            var videoIndex = "normal-0";
            videoId = $"{aircraftSn}/{cameraType}/{videoIndex}";
        }

        var payload = JsonSerializer.Serialize(new
        {
            tid = Guid.NewGuid().ToString(),
            bid = Guid.NewGuid().ToString(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            gateway = req.GatewaySn,
            method = "live_lens_change",
            data = new
            {
                video_id = videoId,
                video_type = req.LensType
            }
        });

        try
        {
            await mqtt.PublishAsync($"thing/product/{req.GatewaySn}/services", payload, MqttQualityOfServiceLevel.AtLeastOnce);
            admin.AddLog("INFO", "LiveStream", $"live_lens_change → {req.GatewaySn} | lens={req.LensType} | video_id={videoId}");
            return Ok(new { success = true, lens = req.LensType, videoId });
        }
        catch (Exception ex)
        {
            admin.AddLog("WARN", "LiveStream", $"live_lens_change MQTT excepción: {ex.Message}");
            return StatusCode(500, new { error = $"Error al enviar el comando MQTT: {ex.Message}" });
        }
    }
}

public record StartStreamRequest(string Url);

public record StartLiveRequest(
    string GatewaySn,
    string? VideoId,
    int Quality = 3,
    string CameraType = "0-0-0",
    string? LocalIp = null,
    string VideoIndex = "normal-0",
    bool UseWebRtc = false
);

public record ChangeLensRequest(
    string GatewaySn,
    string LensType,
    string? CameraType = null
);
