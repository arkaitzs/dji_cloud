using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
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
    ILogger<StreamController> logger) : ControllerBase
{
    private const int BaseRtmpPort = 1935;  // primer puerto del pool

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
    /// Prepara el servidor para recibir el vídeo del dron:
    /// 1. Asigna un puerto libre del pool 1935-1954 (uno por gateway).
    /// 2. Arranca ffmpeg en modo receptor RTMP en ese puerto.
    /// 3. Genera un HLS único por dron: /hls/live-{gatewaySn}.m3u8
    /// 4. Publica live_start_push por MQTT al gateway.
    /// </summary>
    [HttpPost("start-live")]
    public async Task<IActionResult> StartLive([FromBody] StartLiveRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.GatewaySn))
            return BadRequest(new { error = "Se requiere el SN del gateway (mando o dock)." });

        // Obtener IP y validar que sea una IPv4 real de red local (nunca localhost ni 0.0.0.0)
        var localIp = !string.IsNullOrWhiteSpace(req.LocalIp) ? req.LocalIp : GetLocalIp();
        if (localIp is "127.0.0.1" or "::1" or "0.0.0.0" or "localhost")
            localIp = GetLocalIp();
        if (!IPAddress.TryParse(localIp, out var parsedIp) ||
            parsedIp.AddressFamily != AddressFamily.InterNetwork ||
            IPAddress.IsLoopback(parsedIp))
            return BadRequest(new { error = $"IP local inválida '{localIp}'. Especifícala manualmente en el campo 'IP del servidor'." });

        var hlsDir  = Path.Combine(env.WebRootPath, "hls");

        // 1 · Calcular videoId ANTES de arrancar ffmpeg (se almacena en StreamInfo)
        var aircraftSn = admin.GetAircraftForGateway(req.GatewaySn) ?? req.GatewaySn;
        var cameraType = string.IsNullOrWhiteSpace(req.CameraType) ? "0-0-0" : req.CameraType;
        var videoIndex = string.IsNullOrWhiteSpace(req.VideoIndex) ? "normal-0" : req.VideoIndex;
        var videoId    = !string.IsNullOrWhiteSpace(req.VideoId)
            ? req.VideoId
            : $"{aircraftSn}/{cameraType}/{videoIndex}";

        // 2 · Arrancar (o reemplazar) el proceso ffmpeg para este gateway
        var info = await ffmpeg.StartRtmpListenerAsync(
            req.GatewaySn, localIp, BaseRtmpPort, hlsDir,
            videoId: videoId, aircraftSn: aircraftSn);
        if (info is null)
            return StatusCode(500, new
            {
                error = $"No se pudo iniciar ffmpeg para {req.GatewaySn}. " +
                        "Comprueba que ffmpeg está en el PATH y que no hay más de 20 streams activos."
            });

        // 3 · Construir RTMP URL con el puerto asignado
        var rtmpUrl = $"rtmp://{localIp}:{info.Port}/live/drone";

        // 4 · Verificar conexión MQTT antes de construir el payload
        if (!mqtt.IsConnected)
        {
            logger.LogError("[MQTT] ❌ Broker MQTT desconectado. Abortando live_start_push para {GatewaySn}.", req.GatewaySn);
            return StatusCode(503, new { error = "El broker MQTT no está conectado. Reinicia el servidor." });
        }

        // 5 · Construir y loggear el payload completo antes de enviarlo
        var tid = Guid.NewGuid().ToString();
        var bid = Guid.NewGuid().ToString();
        var ts  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var quality = req.Quality >= 0 && req.Quality <= 4 ? req.Quality : 3;

        var payload = JsonSerializer.Serialize(new
        {
            tid,
            bid,
            timestamp = ts,
            gateway   = req.GatewaySn,   // campo raíz obligatorio según DJI Cloud API Common Fields
            method    = "live_start_push",
            data      = new
            {
                url_type      = 1,
                url           = rtmpUrl,
                video_id      = videoId,
                video_quality = quality
            }
        });

        var topicGateway = $"thing/product/{req.GatewaySn}/services";

        logger.LogInformation(
            "[MQTT] >>> live_start_push\n  topic : {Topic}\n  url   : {Url}\n  vidId : {VideoId}\n  payload: {Payload}",
            topicGateway, rtmpUrl, videoId, payload);

        try
        {
            await mqtt.PublishAsync(topicGateway, payload, MqttQualityOfServiceLevel.AtLeastOnce);
            admin.AddLog("INFO", "LiveStream",
                $"live_start_push → {req.GatewaySn} | rtmp={rtmpUrl} | video_id={videoId} | port={info.Port}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MQTT] ❌ Excepción al publicar live_start_push → {GatewaySn}", req.GatewaySn);
            admin.AddLog("WARN", "LiveStream", $"live_start_push MQTT excepción: {ex.Message}");
            return StatusCode(503, new { error = $"Error MQTT al enviar live_start_push: {ex.Message}" });
        }

        return Ok(new
        {
            hlsUrl    = info.HlsPath,
            rtmpUrl,
            localIp,
            rtmpPort  = info.Port,
            videoId,
            aircraftSn,
            mqttTopic = topicGateway
        });
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
        // Recuperar video_id ANTES de parar el proceso
        var info = ffmpeg.GetBySn(gatewaySn);

        // Detener ffmpeg (kill + esperar puerto libre)
        await ffmpeg.StopAsync(gatewaySn);

        // Publicar live_stop_push con el video_id del stream que acábamos de parar
        if (!string.IsNullOrWhiteSpace(gatewaySn))
        {
            try
            {
                var videoId = info?.VideoId ?? "";
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

        return Ok(new { stopped = true, gatewaySn, port = info?.Port, videoId = info?.VideoId });
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
    string VideoIndex = "normal-0"
);

public record ChangeLensRequest(
    string GatewaySn,
    string LensType,
    string? CameraType = null
);
