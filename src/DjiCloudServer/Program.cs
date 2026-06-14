using DjiCloudServer;
using DjiCloudServer.Services;
using DjiCloudServer.Hubs;
using DjiCloudServer.Models;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.AspNetCore;
using MQTTnet.Server;
using MQTTnet.Protocol;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Soporte de Windows Service (graceful start/stop cuando se ejecuta como servicio)
builder.Host.UseWindowsService(options =>
    options.ServiceName = "DjiCloudServer");

builder.WebHost.ConfigureKestrel(options =>
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (!string.IsNullOrEmpty(urls))
    {
        foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            // Parsear robustamente el esquema y el puerto sin usar 'new Uri()'
            // que falla con wildcards del tipo 'http://*' o 'http://+'
            var parts = url.Split(':');
            if (parts.Length >= 2)
            {
                var scheme = parts[0];
                var portStr = parts[^1].TrimEnd('/');
                if (int.TryParse(portStr, out var port))
                {
                    if (scheme.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                    {
                        options.ListenAnyIP(port, l => l.UseHttps());
                    }
                    else
                    {
                        options.ListenAnyIP(port);
                    }
                }
            }
        }
    }
    else
    {
        options.ListenAnyIP(5072);
    }

    options.ListenAnyIP(1883, l => l.UseMqtt());
});

// ─── Controladores API ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// ─── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "DJI Cloud API Server",
        Version     = "v1",
        Description = "Backend server compatible with DJI Cloud API v1.11.x — supports DJI Pilot 2 and DJI Dock."
    });
});

// ─── CORS (DJI Pilot 2 / WebSocket desde la app del dron) ────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DjiPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ─── Servicios DJI Cloud API ──────────────────────────────────────────────────
builder.Services.AddSingleton<IMapDataService, MapDataService>();
builder.Services.AddSingleton<IDjiWebSocketManager, DjiWebSocketManager>();
builder.Services.AddSingleton<IMapSyncNotifier, MapSyncNotifier>();
builder.Services.AddSingleton<IEventLogService, EventLogService>();   // #2.8: logs con buffers por categoría
builder.Services.AddSingleton<IAdminDataService, AdminDataService>();
builder.Services.AddSingleton<ITrajectoryStore, TrajectoryStore>();
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddHostedService(sp => (MqttService)sp.GetRequiredService<IMqttService>());
builder.Services.AddSingleton<IFfmpegService, FfmpegService>();
builder.Services.AddSingleton<IFlightRecorderService, FlightRecorderService>();
builder.Services.AddHostedService(sp => (FlightRecorderService)sp.GetRequiredService<IFlightRecorderService>());
builder.Services.AddSingleton<IMqttFileLogger, MqttFileLogger>();
builder.Services.AddHostedService(sp => (MqttFileLogger)sp.GetRequiredService<IMqttFileLogger>());


// Permitir subidas grandes (vídeos)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 4L * 1024 * 1024 * 1024; // 4 GB
});

// Configurar el Broker MQTT Embebido
builder.Services.AddMqttServer(options => {
    options.WithDefaultEndpointPort(1883);
});
builder.Services.AddConnections();
builder.Services.AddSignalR();

// ─── Configuración de appsettings ────────────────────────────────────────────
builder.Services.Configure<DjiCloudOptions>(
    builder.Configuration.GetSection("DjiCloud"));

// ─── Logging ──────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Limpiar procesos ffmpeg huérfanos de reinicios anteriores
await app.Services.GetRequiredService<IFfmpegService>().KillOrphansAsync();

// Abrir el rango de puertos RTMP en el Firewall de Windows (silencioso si no hay permisos)
OpenRtmpFirewallRule(app.Logger);

// ─── Pipeline HTTP ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DJI Cloud API v1");
        c.RoutePrefix = "swagger";
    });
}

// Los tipos de dispositivo y el mapeo RC↔aeronave ahora se almacenan en IAdminDataService

// Registro del cooldown para auto-solicitud de live_capacity
var lastCapacityRequestTimes = new System.Collections.Concurrent.ConcurrentDictionary<string, long>();

// Levantar el servidor MQTT con interceptor de telemetría
app.UseMqttServer(server => {
    // Diccionario temporal: ClientId → IP de origen (capturado en ValidatingConnection)
    var pendingClientIps = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

    server.ValidatingConnectionAsync += e => {
        // Extraer IP de origen del endpoint (formato "192.168.x.y:port")
        var rawEndpoint = e.Endpoint ?? "";
        var clientIp    = rawEndpoint.Contains(':') ? rawEndpoint[..rawEndpoint.LastIndexOf(':')] : rawEndpoint;
        if (!string.IsNullOrEmpty(clientIp) && !string.IsNullOrEmpty(e.ClientId))
            pendingClientIps[e.ClientId] = clientIp;

        app.Logger.LogDebug("[MQTT] Cliente conectando. ClientId='{ClientId}' IP={ClientIp}", e.ClientId, clientIp);
        
        var fileLogger = app.Services.GetRequiredService<IMqttFileLogger>();
        fileLogger.Log(e.ClientId ?? "Unknown", "CONNECTING", payload: $"Endpoint: {e.Endpoint}, Username: {e.UserName}");

        e.ReasonCode = MqttConnectReasonCode.Success;
        return Task.CompletedTask;
    };

    server.ClientConnectedAsync += e => {
        app.Logger.LogInformation("[MQTT] Cliente conectado. ClientId='{ClientId}'", e.ClientId);

        var fileLogger = app.Services.GetRequiredService<IMqttFileLogger>();
        fileLogger.Log(e.ClientId ?? "Unknown", "CONNECTED");

        var options = app.Services.GetRequiredService<IOptions<DjiCloudOptions>>().Value;
        var serverClientId = options.Mqtt?.ClientId ?? "DjiCloudServer";
        if (e.ClientId == serverClientId)
        {
            return Task.CompletedTask;
        }

        var adminData = app.Services.GetRequiredService<IAdminDataService>();
        adminData.RecordMqttEvent(e.ClientId ?? "Unknown", "", "CONNECTED", "Cliente conectado al broker MQTT");
        adminData.SetDeviceOnlineState(e.ClientId ?? "Unknown", e.ClientId ?? "Unknown", "Cliente MQTT", true);

        // Persistir la IP de origen capturada en ValidatingConnectionAsync
        if (e.ClientId != null && pendingClientIps.TryRemove(e.ClientId, out var ip) && !string.IsNullOrEmpty(ip))
        {
            adminData.SetDeviceClientIp(e.ClientId, ip);
            app.Logger.LogDebug("[MQTT] IP origen registrada. ClientId='{ClientId}' IP={ClientIp}", e.ClientId, ip);
        }

        // TSA (Situational Awareness) — spec DJI doc 58:
        // Push device_online con payload TopologyDeviceDTO (como pushDeviceOnlineTopo
        // de la demo Java) → el RC llama a GET /manage/api/.../devices/topologies.
        // Filtrar clientes DRC ("drc-...") y clientes genéricos sin SN real.
        var clientIdStr = e.ClientId ?? "";
        if (!clientIdStr.StartsWith("drc-", StringComparison.Ordinal))
        {
            var connTypeCode = adminData.GetDeviceTypeCode(clientIdStr);
            var connSubType  = adminData.GetDeviceSubtypeCode(clientIdStr);
            var connDomain   = adminData.IsGateway(clientIdStr) ? 2 : 0;  // dominio 2 = RC/gateway
            var tsaNotifier  = app.Services.GetRequiredService<IMapSyncNotifier>();
            _ = tsaNotifier.NotifyDeviceOnlineAsync(new
            {
                sn              = clientIdStr,
                online_status   = true,
                device_callsign = clientIdStr,
                user_id         = "pilot",
                user_callsign   = clientIdStr,
                domain          = connDomain.ToString(),
                device_model    = new
                {
                    domain   = connDomain.ToString(),
                    type     = (connTypeCode > 0 ? connTypeCode : 0).ToString(),
                    sub_type = connSubType.ToString(),
                    key      = $"{connDomain}-{(connTypeCode > 0 ? connTypeCode : 0)}-{connSubType}"
                },
                gateway_sn      = ""
            });
            app.Logger.LogDebug("[TSA] device_online WS push → ClientId={ClientId}", clientIdStr);
        }

        return Task.CompletedTask;
    };

    // Registrar qué topics suscribe cada cliente — diagnóstico crítico
    server.ClientSubscribedTopicAsync += e => {
        app.Logger.LogDebug("[MQTT] Suscripción. ClientId='{ClientId}' Topic={Topic} QoS={Qos}", e.ClientId, e.TopicFilter.Topic, (int)e.TopicFilter.QualityOfServiceLevel);

        var fileLogger = app.Services.GetRequiredService<IMqttFileLogger>();
        fileLogger.Log(e.ClientId ?? "Unknown", "SUBSCRIBE", topic: e.TopicFilter.Topic, payload: $"QoS: {e.TopicFilter.QualityOfServiceLevel}");

        var adminSub = app.Services.GetRequiredService<IAdminDataService>();
        adminSub.AddLog("INFO", "MQTT-Sub", $"{e.ClientId} se suscribió a {e.TopicFilter.Topic}");
        return Task.CompletedTask;
    };

    server.ClientUnsubscribedTopicAsync += e => {
        app.Logger.LogDebug("[MQTT] Baja de suscripción. ClientId='{ClientId}' Topic={Topic}", e.ClientId, e.TopicFilter);

        var fileLogger = app.Services.GetRequiredService<IMqttFileLogger>();
        fileLogger.Log(e.ClientId ?? "Unknown", "UNSUBSCRIBE", topic: e.TopicFilter);

        return Task.CompletedTask;
    };

    server.ClientDisconnectedAsync += e => {
        app.Logger.LogInformation("[MQTT] Cliente desconectado. ClientId='{ClientId}'", e.ClientId);

        var fileLogger = app.Services.GetRequiredService<IMqttFileLogger>();
        fileLogger.Log(e.ClientId ?? "Unknown", "DISCONNECTED", payload: $"Type: {e.DisconnectType}");

        var options = app.Services.GetRequiredService<IOptions<DjiCloudOptions>>().Value;
        var serverClientId = options.Mqtt?.ClientId ?? "DjiCloudServer";
        if (e.ClientId == serverClientId)
        {
            return Task.CompletedTask;
        }

        var adminData = app.Services.GetRequiredService<IAdminDataService>();
        adminData.RecordMqttEvent(e.ClientId ?? "Unknown", "", "DISCONNECTED", "Cliente desconectado del broker MQTT");
        adminData.SetDeviceOnlineState(e.ClientId ?? "Unknown", e.ClientId ?? "Unknown", "Cliente MQTT", false);

        // TSA — spec DJI doc 58: push device_offline {sn, online_status:false}
        // (como pushDeviceOfflineTopo de la demo Java) → el RC actualiza su mapa
        var discClientId = e.ClientId ?? "";
        if (!discClientId.StartsWith("drc-", StringComparison.Ordinal))
        {
            var tsaNotifier = app.Services.GetRequiredService<IMapSyncNotifier>();
            _ = tsaNotifier.NotifyDeviceOfflineAsync(discClientId);
            app.Logger.LogDebug("[TSA] device_offline WS push → ClientId={ClientId}", discClientId);
        }

        return Task.CompletedTask;
    };

    server.InterceptingPublishAsync += async e => {
        var topic = e.ApplicationMessage.Topic;
        var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        // ── ACL guard ──────────────────────────────────────────────────────────
        // En algunas versiones de MQTTnet el default de ProcessPublish puede ser false
        // cuando hay handlers registrados. Lo forzamos a true antes de cualquier lógica
        // para evitar bloqueos silenciosos; cualquier denegación explícita se loguea.
        if (!e.ProcessPublish)
            app.Logger.LogWarning("[MQTT-ACL] Broker bloqueó publicación de {ClientId} en {Topic}",
                e.ClientId, topic);
        e.ProcessPublish = true;

        // Registrar en el log de tráfico MQTT (incluyendo los del propio servidor)
        var fileLogger = app.Services.GetRequiredService<IMqttFileLogger>();
        fileLogger.Log(e.ClientId ?? "Server", "PUBLISH", topic: topic, payload: payload);

        var options = app.Services.GetRequiredService<IOptions<DjiCloudOptions>>().Value;
        var serverClientId = options.Mqtt?.ClientId ?? "DjiCloudServer";
        if (e.ClientId == serverClientId)
        {
            return;
        }

        // ── ACL: excepción para clientes DRC ──────────────────────────────────
        // ClientId formato "drc-{SNfrag}-{ts}": publica OSD bajo el SN de la aeronave,
        // no bajo su propio ClientId. Sin esta excepción un ACL estricto los bloquearía.
        if ((e.ClientId ?? "").StartsWith("drc-") &&
            (topic.Contains("/osd") || topic.Contains("/state") || topic.Contains("/drc/")))
        {
            e.ProcessPublish = true;
        }

        // Catch-all: loggear TODOS los topics recibidos (excluir telemetría de alta frecuencia)
        if (!topic.Contains("/osd") && !topic.Contains("/state"))
            app.Logger.LogDebug("[MQTT-RECV] topic={Topic} | cliente={Client}", topic, e.ClientId);
        // Log destacado para topics de reply (diagnóstico de ACK)
        if (topic.Contains("/services_reply") || topic.Contains("/events"))
            app.Logger.LogInformation("[MQTT-RECV] *** topic={Topic} ***", topic);

        // Si no es un topic super frecuente de telemetría (como osd o status), loguearlo en consola web
        if (!topic.Contains("/osd") && !topic.Contains("/status") && !topic.Contains("/state"))
        {
            var adminData = app.Services.GetRequiredService<IAdminDataService>();
            var snippet = payload.Length > 120 ? payload.Substring(0, 120) + "..." : payload;
            adminData.RecordMqttEvent(e.ClientId ?? "Unknown", topic, "PUBLISH", snippet);
        }
        
        // ── DRC: canal de telemetría de alta frecuencia (10-30 Hz) ───────────
        if (topic.StartsWith("thing/product/") && topic.Contains("/drc/up"))
        {
            var drcParts = topic.Split('/');
            var drcGwSn  = drcParts.Length >= 3 ? drcParts[2] : "";
            try
            {
                using var drcDoc  = JsonDocument.Parse(payload);
                var drcRoot = drcDoc.RootElement;
                var drcMethod = drcRoot.TryGetProperty("method", out var dm) ? dm.GetString() ?? "" : "";

                if (drcMethod == "osd_info_push" &&
                    drcRoot.TryGetProperty("data", out var drcData))
                {
                    // Extraer posición y actitud a alta frecuencia
                    if (drcData.TryGetProperty("latitude",  out var dLat) &&
                        drcData.TryGetProperty("longitude", out var dLon))
                    {
                        double dLatV  = dLat.GetDouble();
                        double dLonV  = dLon.GetDouble();
                        if (dLatV == 0.0 && dLonV == 0.0) goto skipDrc;

                        double dAlt   = drcData.TryGetProperty("height",        out var dh)  ? dh.GetDouble()  : 0;
                        double dHead  = drcData.TryGetProperty("attitude_head",  out var dhd) ? dhd.GetDouble() : 0;
                        double dPitch = drcData.TryGetProperty("attitude_pitch", out var dpt) ? dpt.GetDouble() : 0;
                        double dRoll  = drcData.TryGetProperty("attitude_roll",  out var drl) ? drl.GetDouble() : 0;
                        double dHSpd  = drcData.TryGetProperty("horizontal_speed", out var hs2) ? hs2.GetDouble() : 0;
                        int dBat      = drcData.TryGetProperty("battery", out var dBatEl) &&
                                        dBatEl.TryGetProperty("capacity_percent", out var dBp) ? dBp.GetInt32() : -1;

                        // Determinar SN efectivo de la aeronave
                        var adminDrc = app.Services.GetRequiredService<IAdminDataService>();
                        var drcAcSn  = adminDrc.GetAircraftForGateway(drcGwSn) ?? drcGwSn;

                        // Throttle: publicar al frontend máximo cada 100ms (~10Hz)
                        if (adminDrc.ShouldPushSignalR(drcAcSn + "_drc", 100))
                        {
                            var drcHub = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                            await drcHub.Clients.All.SendCoreAsync("UpdateDronePosition",
                                new object[] { drcAcSn, dLatV, dLonV, dAlt,
                                               dPitch, dRoll, dHead, dHead,
                                               1.0, dBat >= 0 ? dBat : 100, 0, 0.0 });
                        }
                    }
                }
                else if (drcMethod == "drc_camera_osd_info_push" &&
                         drcRoot.TryGetProperty("data", out var camDrcData))
                {
                    var adminDrc2  = app.Services.GetRequiredService<IAdminDataService>();
                    var drcAcSn2   = adminDrc2.GetAircraftForGateway(drcGwSn) ?? drcGwSn;
                    var camIdx     = camDrcData.TryGetProperty("camera_index", out var ci3) ? ci3.GetString() ?? "" : "";
                    double gPitch  = camDrcData.TryGetProperty("gimbal_pitch", out var cgp) ? cgp.GetDouble() : 0;
                    double gRoll   = camDrcData.TryGetProperty("gimbal_roll",  out var cgr) ? cgr.GetDouble() : 0;
                    double gYaw    = camDrcData.TryGetProperty("gimbal_yaw",   out var cgy) ? cgy.GetDouble() : 0;
                    double zoom    = camDrcData.TryGetProperty("zoom_factor",  out var czf) ? czf.GetDouble() : 1;
                    bool   irOn    = camDrcData.TryGetProperty("ir_switch",    out var cir) && cir.ValueKind == JsonValueKind.True;
                    int    recSt   = camDrcData.TryGetProperty("recording_state", out var crs) ? crs.GetInt32() : 0;

                    if (adminDrc2.ShouldPushSignalR(drcAcSn2 + "_cam", 200))
                    {
                        var camHub = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                        await camHub.Clients.All.SendAsync("UpdateCameraState", drcAcSn2,
                            new[] { new { cameraIndex = camIdx, gimbalPitch = gPitch, gimbalRoll = gRoll, gimbalYaw = gYaw,
                                          zoomFactor = zoom, irSwitch = irOn, recordingState = recSt } });
                    }
                }
                skipDrc:;
            }
            catch (Exception ex)
            {
                app.Logger.LogDebug(ex, "[MQTT-DRC] Error parseando mensaje DRC. Topic={Topic}", topic);
            }
        }

        // ── Parsear eventos HMS (Health Management System) + events_reply ─────
        if (topic.StartsWith("thing/product/") && topic.EndsWith("/events"))
        {
            var snEvt = topic.Split('/').ElementAtOrDefault(2) ?? "";
            try
            {
                using var evDoc = JsonDocument.Parse(payload);
                var evRoot = evDoc.RootElement;

                // ── events_reply obligatorio cuando need_reply=1 (doc 27) ──────────
                // Sin esta respuesta el dispositivo reintenta el evento o bloquea el
                // flujo que lo originó (p.ej. ciclo de subida de media).
                if (evRoot.TryGetProperty("need_reply", out var nrEl)
                    && nrEl.ValueKind == JsonValueKind.Number && nrEl.GetInt32() == 1)
                {
                    var evTid    = evRoot.TryGetProperty("tid",    out var evT) ? evT.GetString() ?? "" : "";
                    var evBid    = evRoot.TryGetProperty("bid",    out var evB) ? evB.GetString() ?? "" : "";
                    var evMethod = evRoot.TryGetProperty("method", out var evM) ? evM.GetString() ?? "" : "";

                    var evReplyJson = JsonSerializer.Serialize(new
                    {
                        tid       = evTid,
                        bid       = evBid,
                        method    = evMethod,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        data      = new { result = 0 }
                    });
                    var evReplyMsg = new MqttApplicationMessageBuilder()
                        .WithTopic($"thing/product/{snEvt}/events_reply")
                        .WithPayload(System.Text.Encoding.UTF8.GetBytes(evReplyJson))
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();
                    await server.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(evReplyMsg) { SenderClientId = serverClientId });
                    app.Logger.LogInformation("[MQTT] events_reply [{Method}] enviado → {Sn} (need_reply=1)",
                        evMethod, snEvt);
                }
                if (evRoot.TryGetProperty("data",   out var evData)   &&
                    evData.TryGetProperty("event",  out var evType)   &&
                    evType.GetString() == "hms"                        &&
                    evData.TryGetProperty("output", out var evOutput) &&
                    evOutput.TryGetProperty("codes", out var codesEl) &&
                    codesEl.ValueKind == JsonValueKind.Array)
                {
                    var codes = new List<HmsCodeDto>();
                    foreach (var item in codesEl.EnumerateArray())
                    {
                        codes.Add(new HmsCodeDto
                        {
                            ComponentIndex = item.TryGetProperty("component_index", out var ci) ? ci.GetInt32() : 0,
                            Code           = item.TryGetProperty("code",            out var c)  ? c.GetInt64()  : 0,
                            Level          = item.TryGetProperty("level",           out var lv) ? lv.GetInt32() : 0
                        });
                    }
                    var adminEvt = app.Services.GetRequiredService<IAdminDataService>();
                    adminEvt.SetHmsCodes(snEvt, codes);
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "[MQTT-HMS] Error parseando evento HMS. SN={Sn}", snEvt);
            }
        }

        // ── requests → requests_reply (doc 27) ─────────────────────────────────
        // El dispositivo pide información al servidor (p.ej. credenciales temporales
        // de almacenamiento con method=storage_config_get). Sin respuesta, el ciclo
        // que originó la petición (subida de media, etc.) queda bloqueado en el RC.
        if (topic.StartsWith("thing/product/") && topic.EndsWith("/requests"))
        {
            var snReq = topic.Split('/').ElementAtOrDefault(2) ?? "";
            try
            {
                using var reqDoc = JsonDocument.Parse(payload);
                var reqRoot   = reqDoc.RootElement;
                var reqTid    = reqRoot.TryGetProperty("tid",    out var rqT) ? rqT.GetString() ?? "" : "";
                var reqBid    = reqRoot.TryGetProperty("bid",    out var rqB) ? rqB.GetString() ?? "" : "";
                var reqMethod = reqRoot.TryGetProperty("method", out var rqM) ? rqM.GetString() ?? "" : "";

                // output según el method solicitado
                object reqOutput;
                if (reqMethod == "storage_config_get")
                {
                    // Misma configuración mock que POST /storage/api/v1/.../sts (MediaController)
                    var stsServerIp = !string.IsNullOrWhiteSpace(options.ServerIp) ? options.ServerIp : "192.168.1.150";
                    reqOutput = new
                    {
                        bucket      = "local-media-bucket",
                        credentials = new
                        {
                            access_key_id     = "mock_access_key_id",
                            access_key_secret = "mock_access_key_secret",
                            expire            = 3600,
                            security_token    = "mock_security_token"
                        },
                        endpoint          = $"http://{stsServerIp}:5072/api/media/mock-s3",
                        object_key_prefix = "media",
                        provider          = "minio",
                        region            = "local-lan"
                    };
                }
                else
                {
                    reqOutput = new { };
                }

                var reqReplyJson = JsonSerializer.Serialize(new
                {
                    tid       = reqTid,
                    bid       = reqBid,
                    method    = reqMethod,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    data      = new { result = 0, output = reqOutput }
                });
                var reqReplyMsg = new MqttApplicationMessageBuilder()
                    .WithTopic($"thing/product/{snReq}/requests_reply")
                    .WithPayload(System.Text.Encoding.UTF8.GetBytes(reqReplyJson))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();
                await server.InjectApplicationMessage(
                    new InjectedMqttApplicationMessage(reqReplyMsg) { SenderClientId = serverClientId });
                app.Logger.LogInformation("[MQTT] requests_reply [{Method}] enviado → {Sn}", reqMethod, snReq);
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "[MQTT] Error procesando requests. SN={Sn}", snReq);
            }
        }

        // Interceptamos: services_reply (respuesta de DJI Pilot 2 a comandos de servicio)
        if (topic.StartsWith("thing/product/") && topic.EndsWith("/services_reply"))
        {
            var parts = topic.Split('/');
            if (parts.Length >= 3)
            {
                var gatewaySn = parts[2];
                try
                {
                    using var replyDoc = JsonDocument.Parse(payload);
                    var replyRoot = replyDoc.RootElement;
                    var method    = replyRoot.TryGetProperty("method",    out var mEl)    ? mEl.GetString()    ?? "" : "";
                    var result    = replyRoot.TryGetProperty("data",      out var dEl)    &&
                                   dEl.TryGetProperty("result",           out var rEl)    &&
                                   rEl.ValueKind == JsonValueKind.Number  ? rEl.GetInt32() : -1;
                    var adminReply = app.Services.GetRequiredService<IAdminDataService>();
                    adminReply.SetLastServicesReply(gatewaySn, method, result);
                    if (method == "live_start_push")
                    {
                        var resultDesc = result == 0
                            ? "OK — stream aceptado por el dron"
                            : $"ERROR código {result} — el dron rechazó el comando";
                        app.Logger.LogInformation(
                            "[STREAMING] *** ACK del dron recibido. Resultado: {ResultDesc} | gateway={GatewaySn} ***",
                            resultDesc, gatewaySn);

                        var hub = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                        _ = hub.Clients.All.SendAsync("StreamAck", gatewaySn, result);
                    }
                    else if (method == "drc_mode_enter")
                    {
                        app.Logger.LogInformation(
                            "[DRC] drc_mode_enter ACK gateway={GatewaySn} result={Result}",
                            gatewaySn, result);

                        if (result == 0)
                        {
                            // Despertar el flujo DRC: el dron no emite osd_info_push hasta que
                            // el servidor envía drc_initial_state_subscribe por el canal drc/down.
                            // Esperamos 800 ms para dar tiempo al cliente DRC a conectarse al broker.
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    // #2.9: cancelable en el shutdown de la aplicación
                                    await Task.Delay(800, app.Lifetime.ApplicationStopping);
                                    var mqttDrc = app.Services.GetRequiredService<IMqttService>();
                                    var subPayload = JsonSerializer.Serialize(new
                                    {
                                        method = "drc_initial_state_subscribe",
                                        data   = new { }
                                    });
                                    await mqttDrc.PublishAsync(
                                        $"thing/product/{gatewaySn}/drc/down",
                                        subPayload,
                                        MqttQualityOfServiceLevel.AtLeastOnce);
                                    app.Services.GetRequiredService<IAdminDataService>()
                                        .AddLog("INFO", "DRC",
                                            $"drc_initial_state_subscribe → {gatewaySn}");
                                    app.Logger.LogInformation(
                                        "[DRC] drc_initial_state_subscribe enviado → {GatewaySn}", gatewaySn);
                                }
                                catch (OperationCanceledException)
                                {
                                    // Apagado de la aplicación durante el delay — no es un error
                                }
                                catch (Exception exDrc)
                                {
                                    app.Logger.LogWarning(exDrc,
                                        "[DRC] Error enviando drc_initial_state_subscribe → {GatewaySn}",
                                        gatewaySn);
                                }
                            });
                        }
                        else
                        {
                            app.Logger.LogWarning(
                                "[DRC] drc_mode_enter RECHAZADO por el dron. gateway={GatewaySn} código={Result}",
                                gatewaySn, result);
                        }
                    }
                    else if (method == "live_lens_change")
                    {
                        var resultDesc = result == 0
                            ? "OK — lente cambiada en el dron"
                            : $"ERROR código {result} — el dron rechazó el cambio de lente";
                        app.Logger.LogInformation(
                            "[STREAMING] live_lens_change ACK gateway={GatewaySn} result={ResultDesc}",
                            gatewaySn, resultDesc);

                        var hubLens = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                        _ = hubLens.Clients.All.SendAsync("LensChangeAck", gatewaySn, result);
                    }
                    else if (method == "live_set_quality")
                    {
                        var resultDesc = result == 0
                            ? "OK — calidad aplicada"
                            : $"ERROR código {result} — el dron rechazó el cambio de calidad";
                        app.Logger.LogInformation(
                            "[STREAMING] live_set_quality ACK gateway={GatewaySn} result={ResultDesc}",
                            gatewaySn, resultDesc);

                        var hubQ = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                        _ = hubQ.Clients.All.SendAsync("SetQualityAck", gatewaySn, result);
                    }
                    else
                    {
                        app.Logger.LogDebug("[MQTT] services_reply [{Method}] gw={GatewaySn} result={Result}",
                            method, gatewaySn, result);
                    }

                    // Parsear live_capacity si viene en este services_reply
                    if ((method == "live_capacity" || method == "get_live_capacity") &&
                        replyRoot.TryGetProperty("data",   out var capData)   &&
                        capData.TryGetProperty("output",   out var capOutput))
                    {
                        var capEl = capOutput.ValueKind == JsonValueKind.Object &&
                                    capOutput.TryGetProperty("live_capacity", out var lc) ? lc : capOutput;
                        var parsed = ParseLiveCapacity(gatewaySn, capEl);
                        if (parsed != null) adminReply.SetLiveCapacity(gatewaySn, parsed);
                    }
                }
                catch (Exception ex)
                {
                    app.Logger.LogWarning(ex, "[MQTT] Error parseando services_reply. Gateway={GatewaySn}", gatewaySn);
                }
            }
        }

        // Interceptamos: status (handshake), osd (telemetría), state (mapeo RC↔aeronave)
        if ((topic.StartsWith("sys/product/")  && topic.EndsWith("/status")) ||
            (topic.StartsWith("thing/product/") && topic.EndsWith("/osd"))   ||
            (topic.StartsWith("thing/product/") && topic.EndsWith("/state")))
        {
            var topicParts = topic.Split('/');
            if (topicParts.Length >= 3)
            {
                var sn        = topicParts[2]; // Número de serie del dron
                var adminData = app.Services.GetRequiredService<IAdminDataService>();

                if (topic.EndsWith("/state"))
                {
                    adminData.SetLastStatePayload(sn, payload);
                }

                // Auto-solicitud de live_capacity si está vacía o no tiene cámaras
                var gatewaySnForCap = sn;
                if (!adminData.IsGateway(sn)) // Si es aeronave, buscar su RC/Gateway
                {
                    // #3.6: mapeo correcto aeronave→gateway (antes se usaba
                    // GetAircraftForGateway, el sentido inverso, y solo funcionaba
                    // por el fallback de búsqueda en GetGateways)
                    gatewaySnForCap = adminData.GetGatewayForAircraft(sn) ??
                                      adminData.GetGateways().FirstOrDefault(g => g.AircraftSn == sn)?.GatewaySn ??
                                      sn;
                }

                if (!string.IsNullOrEmpty(gatewaySnForCap))
                {
                    var cap = adminData.GetLiveCapacity(gatewaySnForCap);
                    var hasCameras = cap?.DeviceList?.Any(d => d.CameraList != null && d.CameraList.Count > 0) == true;
                    if (!hasCameras)
                    {
                        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var lastRequest = lastCapacityRequestTimes.GetOrAdd(gatewaySnForCap, 0L);
                        if (nowMs - lastRequest > 15000) // 15s cooldown
                        {
                            lastCapacityRequestTimes[gatewaySnForCap] = nowMs;
                            // #2.9: cancelable en shutdown — el Task.Run no queda huérfano
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    if (app.Lifetime.ApplicationStopping.IsCancellationRequested) return;
                                    var options = app.Services.GetRequiredService<IOptions<DjiCloudOptions>>().Value;
                                    var serverClientId = options.Mqtt?.ClientId ?? "DjiCloudServer";
                                    var mqttSvc = app.Services.GetRequiredService<IMqttService>();
                                    var capTid = Guid.NewGuid().ToString();
                                    var capBid = Guid.NewGuid().ToString();
                                    var capReq = JsonSerializer.Serialize(new
                                    {
                                        tid       = capTid,
                                        bid       = capBid,
                                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                        gateway   = gatewaySnForCap,
                                        method    = "live_capacity",
                                        data      = new {}
                                    });
                                    // Publicar a través del server inyectando el mensaje
                                    var capMsg = new MqttApplicationMessageBuilder()
                                        .WithTopic($"thing/product/{gatewaySnForCap}/services")
                                        .WithPayload(System.Text.Encoding.UTF8.GetBytes(capReq))
                                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                        .Build();
                                    await server.InjectApplicationMessage(
                                        new InjectedMqttApplicationMessage(capMsg) { SenderClientId = serverClientId });

                                    app.Logger.LogDebug("[MQTT] Auto-solicitando live_capacity. Gateway={GatewaySn}", gatewaySnForCap);
                                    adminData.AddLog("INFO", "LiveStream", $"Auto-solicitando live_capacity a {gatewaySnForCap} (vacío o pendiente)");
                                }
                                catch (Exception ex)
                                {
                                    app.Logger.LogWarning(ex, "[MQTT] Error al auto-solicitar live_capacity. Gateway={GatewaySn}", gatewaySnForCap);
                                }
                            });
                        }
                    }
                }

                try
                {
                    using var jsonDoc = JsonDocument.Parse(payload);
                    var root = jsonDoc.RootElement;

                    // ── Handshake DJI Cloud API: responder a update_topo ──────────────────
                    // Sin este status_reply el RC/gateway no envía telemetría de la aeronave.
                    if (root.TryGetProperty("method", out var methodEl) && methodEl.GetString() == "update_topo")
                    {
                        var tid = root.TryGetProperty("tid", out var tidEl) ? tidEl.GetString() ?? "" : "";
                        var bid = root.TryGetProperty("bid", out var bidEl) ? bidEl.GetString() ?? "" : "";

                        // El emisor de update_topo (sys/product/{gateway_sn}/status) ES el gateway/mando,
                        // sea cual sea su type code (144=RC Pro, 174=RC nuevo, etc.). Lo marcamos
                        // explícitamente para no depender de un número de tipo hardcodeado.
                        adminData.MarkGateway(sn);
                        if (root.TryGetProperty("data", out var topoData))
                        {
                            if (topoData.TryGetProperty("type", out var typeEl) &&
                                typeEl.ValueKind == JsonValueKind.Number)
                            {
                                var gwTypeCode = typeEl.GetInt32();
                                adminData.SetDeviceTypeCode(sn, gwTypeCode);
                                var gwSubType = topoData.TryGetProperty("sub_type", out var gwStEl) && gwStEl.ValueKind == JsonValueKind.Number
                                    ? gwStEl.GetInt32() : 0;
                                // INFO siempre visible: identifica el type_code del mando (144/174/...)
                                app.Logger.LogInformation("[TOPO] Gateway SN={Sn} type={Type} sub_type={SubType} (mando)",
                                    sn, gwTypeCode, gwSubType);
                                adminData.AddLog("INFO", "Topología", $"Gateway {sn}: type={gwTypeCode} sub_type={gwSubType}");
                            }
                            if (topoData.TryGetProperty("sub_type", out var subTypeEl) &&
                                subTypeEl.ValueKind == JsonValueKind.Number)
                            {
                                adminData.SetDeviceSubtypeCode(sn, subTypeEl.GetInt32());
                            }

                            if (topoData.TryGetProperty("sub_devices", out var subDevicesEl) &&
                                subDevicesEl.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var subDev in subDevicesEl.EnumerateArray())
                                {
                                    if (subDev.TryGetProperty("sn", out var subSnEl) &&
                                        subSnEl.ValueKind == JsonValueKind.String)
                                    {
                                        var subSn = subSnEl.GetString();
                                        if (!string.IsNullOrEmpty(subSn))
                                        {
                                            int acType = -1, acSubType = -1;
                                            if (subDev.TryGetProperty("type", out var subTypeVal) &&
                                                subTypeVal.ValueKind == JsonValueKind.Number)
                                            {
                                                acType = subTypeVal.GetInt32();
                                                adminData.SetDeviceTypeCode(subSn, acType);
                                            }
                                            if (subDev.TryGetProperty("sub_type", out var subSubTypeVal) &&
                                                subSubTypeVal.ValueKind == JsonValueKind.Number)
                                            {
                                                acSubType = subSubTypeVal.GetInt32();
                                                adminData.SetDeviceSubtypeCode(subSn, acSubType);
                                            }
                                            adminData.SetRcAircraftPairing(sn, subSn);
                                            // INFO siempre visible: identifica el type_code de la aeronave (M4T u otros)
                                            app.Logger.LogInformation("[TOPO] Aeronave SN={AcSn} type={Type} sub_type={SubType} pareada con gateway={GwSn}",
                                                subSn, acType, acSubType, sn);
                                            adminData.AddLog("INFO", "Topología", $"Aeronave {subSn}: type={acType} sub_type={acSubType} ← gateway {sn}");

                                            // TSA — como updateTopoOnline → pushDeviceOnlineTopo de la demo Java:
                                            // device_online con la topología completa de la aeronave + gateway_sn
                                            var topoNotifier = app.Services.GetRequiredService<IMapSyncNotifier>();
                                            _ = topoNotifier.NotifyDeviceOnlineAsync(new
                                            {
                                                sn              = subSn,
                                                online_status   = true,
                                                device_callsign = subSn,
                                                user_id         = "pilot",
                                                user_callsign   = subSn,
                                                domain          = "0",
                                                device_model    = new
                                                {
                                                    domain   = "0",
                                                    type     = (acType > 0 ? acType : 0).ToString(),
                                                    sub_type = (acSubType >= 0 ? acSubType : 0).ToString(),
                                                    key      = $"0-{(acType > 0 ? acType : 0)}-{(acSubType >= 0 ? acSubType : 0)}"
                                                },
                                                gateway_sn      = sn
                                            });
                                        }
                                    }
                                }

                                // #7 device_update_topo: sub_devices VACÍO = la aeronave se
                                // desemparejó del gateway sin offline MQTT completo (doc 27,
                                // "Sub-device offline"). Notificar offline de la aeronave
                                // previamente pareada + update_topo para que los RC refresquen.
                                if (subDevicesEl.GetArrayLength() == 0)
                                {
                                    var prevAircraft = adminData.GetAircraftForGateway(sn);
                                    var topoChgNotifier = app.Services.GetRequiredService<IMapSyncNotifier>();
                                    if (!string.IsNullOrEmpty(prevAircraft))
                                    {
                                        _ = topoChgNotifier.NotifyDeviceOfflineAsync(prevAircraft);
                                        app.Logger.LogInformation(
                                            "[TOPO] Aeronave {AcSn} desemparejada del gateway {GwSn} (sub_devices vacío)",
                                            prevAircraft, sn);
                                    }
                                    _ = topoChgNotifier.NotifyDeviceUpdateTopoAsync();
                                }
                            }
                        }

                        var replyJson = JsonSerializer.Serialize(new
                        {
                            tid,
                            bid,
                            method    = "update_topo",
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            data      = new { result = 0 }
                        });

                        var replyMsg = new MqttApplicationMessageBuilder()
                            .WithTopic($"sys/product/{sn}/status_reply")
                            .WithPayload(System.Text.Encoding.UTF8.GetBytes(replyJson))
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();

                        await server.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(replyMsg) { SenderClientId = serverClientId });

                        app.Logger.LogDebug("[MQTT] update_topo ACK enviado. SN={Sn}", sn);

                        // Solicitar live_capacity para conocer cámaras disponibles del dron
                        var capTid = Guid.NewGuid().ToString();
                        var capBid = Guid.NewGuid().ToString();
                        var capReq = JsonSerializer.Serialize(new
                        {
                            tid       = capTid,
                            bid       = capBid,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            gateway   = sn,
                            method    = "live_capacity",
                            data      = new {}
                        });
                        var capMsg = new MqttApplicationMessageBuilder()
                            .WithTopic($"thing/product/{sn}/services")
                            .WithPayload(System.Text.Encoding.UTF8.GetBytes(capReq))
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();
                        await server.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(capMsg) { SenderClientId = serverClientId });
                        app.Logger.LogDebug("[MQTT] live_capacity request enviado. SN={Sn}", sn);
                    }

                    if (root.TryGetProperty("data", out var dataElement))
                    {
                        // ── ESTADO — disponible con o sin GPS ────────────────────────────────

                        // Batería
                        int batteryPercent   = -1;
                        int remainFlightTime = -1;
                        if (dataElement.TryGetProperty("capacity_percent", out var bp))
                            batteryPercent = bp.GetInt32();
                        else if (dataElement.TryGetProperty("battery", out var batEl) && batEl.ValueKind == JsonValueKind.Object)
                        {
                            if (batEl.TryGetProperty("capacity_percent",   out var bp2)) batteryPercent   = bp2.GetInt32();
                            if (batEl.TryGetProperty("remain_flight_time", out var rft)) remainFlightTime = rft.GetInt32();
                        }

                        // Modo de vuelo (mode_code DJI: 0=tierra, 4=waypoints, 5=RTH, 7=hover, 13=landing…)
                        int modeCode = -1;
                        if (dataElement.TryGetProperty("mode_code", out var mc) && mc.ValueKind == JsonValueKind.Number)
                            modeCode = mc.GetInt32();

                        // Estado GPS
                        // M3/M4 series: position_state.is_fixed = 0(no iniciado) 1(buscando) 2(fix OK) 3(fallido)
                        // Solo el valor 2 indica fix real — usar > 0 era incorrecto (marcaba "fix" mientras buscaba)
                        int gpsFixed = 0, gpsCountStatus = 0;
                        if (dataElement.TryGetProperty("position_state", out var posState) && posState.ValueKind == JsonValueKind.Object)
                        {
                            if (posState.TryGetProperty("is_fixed",   out var isFixed) && isFixed.ValueKind == JsonValueKind.Number)
                                gpsFixed = isFixed.GetInt32() == 2 ? 1 : 0; // 2 = Fixing successful
                            if (posState.TryGetProperty("gps_number", out var gpsNs) && gpsNs.ValueKind == JsonValueKind.Number)
                                gpsCountStatus = gpsNs.GetInt32();
                        }
                        if (dataElement.TryGetProperty("gps_number", out var gpsDirectN) && gpsDirectN.ValueKind == JsonValueKind.Number)
                            gpsCountStatus = gpsDirectN.GetInt32();

                        // Velocidad horizontal y rumbo de aeronave (sin GPS)
                        double horizontalSpeed = 0.0, attitudeHead = 0.0;
                        if (dataElement.TryGetProperty("horizontal_speed", out var hs)) horizontalSpeed = hs.GetDouble();
                        if (dataElement.TryGetProperty("attitude_head",    out var ah)) attitudeHead    = ah.GetDouble();
                        else if (dataElement.TryGetProperty("yaw",    out var yawS))   attitudeHead    = yawS.GetDouble();
                        else if (dataElement.TryGetProperty("heading", out var hdgS))  attitudeHead    = hdgS.GetDouble();

                        double verticalSpeed = 0.0;
                        if (dataElement.TryGetProperty("vertical_speed", out var vs)) verticalSpeed = vs.GetDouble();

                        double attitudePitch = 0.0;
                        if (dataElement.TryGetProperty("attitude_pitch", out var ap)) attitudePitch = ap.GetDouble();

                        double attitudeRoll = 0.0;
                        if (dataElement.TryGetProperty("attitude_roll", out var ar)) attitudeRoll = ar.GetDouble();

                        // ── Mapeo RC↔aeronave desde thing/{sn}/state con campo "gateway" ──────
                        if (root.TryGetProperty("gateway", out var gwEl) && gwEl.ValueKind == JsonValueKind.String)
                        {
                            var rcSn = gwEl.GetString()!;
                            if (!string.IsNullOrEmpty(rcSn) && rcSn != sn)
                            {
                                adminData.SetRcAircraftPairing(rcSn, sn);
                            }
                        }

                        // Gateway/mando vs aeronave (independiente del type code)
                        bool isGatewaySn = adminData.IsGateway(sn);

                        // Si es el RC y hay una aeronave pareada, usamos el SN de la aeronave
                        // para que el estado aparezca bajo el SN correcto en el mapa
                        string effectiveSn = sn;
                        var pairedAircraft = adminData.GetAircraftForGateway(sn);
                        // Solo reetiquetar el OSD del mando bajo el SN del dron si ese dron está
                        // realmente ACTIVO (envió OSD propio hace poco). Si está apagado, dejar el
                        // OSD bajo el SN del mando (deviceType=144 → el frontend lo ignora) para no
                        // crear un "dron fantasma" en el contador ni en el mapa.
                        if (isGatewaySn && pairedAircraft is not null && adminData.IsAircraftActive(pairedAircraft))
                            effectiveSn = pairedAircraft;

                        // ── Corrección de GPS fix para cualquier dispositivo ─────────────────────
                        // position_state.is_fixed = 0 en drones no-RTK (siempre 0) y en el mando RC
                        // (campo exclusivo de la aeronave). Si el dispositivo reporta lat/lon válidos
                        // (≠ 0,0) es porque SÍ tiene solución GNSS. Inferimos gpsFixed=1 para evitar
                        // el falso "Sin Fix GNSS" en el HUD.
                        if (gpsFixed == 0)
                        {
                            if (dataElement.TryGetProperty("latitude",  out var latInfer) &&
                                dataElement.TryGetProperty("longitude", out var lonInfer))
                            {
                                if (latInfer.GetDouble() != 0.0 || lonInfer.GetDouble() != 0.0)
                                    gpsFixed = 1;
                            }
                        }

                        // Enviar estado al mapa aunque no haya GPS (batería, modo, satélites).
                        // sendDevType=-2 identifica relay de RC → permite al frontend suprimir
                        // re-renders cuando ya existen datos frescos directos de la aeronave.
                        int sendDevType = (effectiveSn != sn) ? -2 : (isGatewaySn ? 144 : adminData.GetDeviceTypeCode(sn));
                        var hubContextStatus = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                        await hubContextStatus.Clients.All.SendAsync("UpdateDroneStatus",
                            effectiveSn, batteryPercent, remainFlightTime, modeCode,
                            gpsFixed, gpsCountStatus, attitudeHead, horizontalSpeed, sendDevType);

                        // Refrescar LastSeen y batería del RC/gateway (no manda lat/lon propio)
                        if (isGatewaySn)
                            adminData.RefreshDeviceStatus(sn, batteryPercent, modeCode);
                        else
                            // Aeronave enviando su PROPIO OSD: registrarla/refrescarla aunque no
                            // tenga fix GPS, para que aparezca en el dashboard y cuente como activa.
                            adminData.RegisterAircraftFromOsd(sn, batteryPercent, gpsCountStatus, modeCode);

                        // ── POSICIÓN — solo cuando hay fix GPS válido ─────────────────────────
                        if (dataElement.TryGetProperty("latitude", out var lat) &&
                            dataElement.TryGetProperty("longitude", out var lon))
                        {
                            double latitude  = lat.GetDouble();
                            double longitude = lon.GetDouble();

                            // Coordenadas (0,0) = sin fix real — ignorar
                            if (latitude == 0.0 && longitude == 0.0) goto skipPosition;

                            // height  = ASL (altura absoluta sobre el elipsoide terrestre)
                            // elevation = AGL (relativa al punto de despegue del operador)
                            double altitude  = dataElement.TryGetProperty("height",     out var heightProp) ? heightProp.GetDouble() : 0.0;
                            double elevation = dataElement.TryGetProperty("elevation",  out var elevProp)   ? elevProp.GetDouble()   : 0.0;
                            // Fallback: si elevation no viene (dispositivos más viejos), usar altitude como aproximación
                            if (elevation == 0.0 && altitude != 0.0) elevation = altitude;
                            
                            // Extraer gimbal, heading del dron y factor de zoom de la cámara (con fallback múltiple para mayor robustez)
                            double gimbalPitch = -90.0; // Valor por defecto (apuntando hacia abajo)
                            double gimbalRoll = 0.0;
                            double gimbalYaw = 0.0;
                            double heading = 0.0;
                            double zoomFactor = 1.0;

                            // 1. Intentar leer del nivel principal de "data"
                            if (dataElement.TryGetProperty("gimbalPitch", out var gp)) gimbalPitch = gp.GetDouble();
                            else if (dataElement.TryGetProperty("gimbal_pitch", out var gp2)) gimbalPitch = gp2.GetDouble();

                            if (dataElement.TryGetProperty("gimbalRoll", out var gr)) gimbalRoll = gr.GetDouble();
                            else if (dataElement.TryGetProperty("gimbal_roll", out var gr2)) gimbalRoll = gr2.GetDouble();

                            if (dataElement.TryGetProperty("gimbalYaw", out var gy)) gimbalYaw = gy.GetDouble();
                            else if (dataElement.TryGetProperty("gimbal_yaw", out var gy2)) gimbalYaw = gy2.GetDouble();
                            else if (dataElement.TryGetProperty("gimbal_heading", out var gh)) gimbalYaw = gh.GetDouble();

                            // attitude_head es el nombre correcto en DJI Cloud API OSD
                            if (dataElement.TryGetProperty("attitude_head", out var hdgAtt)) heading = hdgAtt.GetDouble();
                            else if (dataElement.TryGetProperty("yaw",     out var hdgYaw)) heading = hdgYaw.GetDouble();
                            else if (dataElement.TryGetProperty("heading",  out var hdgH))  heading = hdgH.GetDouble();
                            else if (dataElement.TryGetProperty("track",    out var hdgT))  heading = hdgT.GetDouble();

                            if (dataElement.TryGetProperty("zoomFactor", out var zf)) zoomFactor = zf.GetDouble();
                            else if (dataElement.TryGetProperty("zoom_factor", out var zf2)) zoomFactor = zf2.GetDouble();
                            else if (dataElement.TryGetProperty("cameraZoomFactor", out var zf3)) zoomFactor = zf3.GetDouble();

                            // 2. Intentar buscar en el objeto "gimbal"
                            if (dataElement.TryGetProperty("gimbal", out var gimbalObj) && gimbalObj.ValueKind == JsonValueKind.Object)
                            {
                                if (gimbalPitch == -90.0)
                                {
                                    if (gimbalObj.TryGetProperty("pitch", out var gp3)) gimbalPitch = gp3.GetDouble();
                                    else if (gimbalObj.TryGetProperty("gimbalPitch", out var gp4)) gimbalPitch = gp4.GetDouble();
                                }
                                if (gimbalRoll == 0.0)
                                {
                                    if (gimbalObj.TryGetProperty("roll", out var gr3)) gimbalRoll = gr3.GetDouble();
                                    else if (gimbalObj.TryGetProperty("gimbalRoll", out var gr4)) gimbalRoll = gr4.GetDouble();
                                }
                                if (gimbalYaw == 0.0)
                                {
                                    if (gimbalObj.TryGetProperty("yaw", out var gy3)) gimbalYaw = gy3.GetDouble();
                                    else if (gimbalObj.TryGetProperty("gimbalYaw", out var gy4)) gimbalYaw = gy4.GetDouble();
                                    else if (gimbalObj.TryGetProperty("gimbal_heading", out var gh2)) gimbalYaw = gh2.GetDouble();
                                }
                            }

                            // 3. Intentar buscar en el objeto "camera"
                            if (dataElement.TryGetProperty("camera", out var camObj) && camObj.ValueKind == JsonValueKind.Object)
                            {
                                if (zoomFactor == 1.0)
                                {
                                    if (camObj.TryGetProperty("zoomFactor", out var zf4)) zoomFactor = zf4.GetDouble();
                                    else if (camObj.TryGetProperty("zoom_factor", out var zf5)) zoomFactor = zf5.GetDouble();
                                }
                            }

                            // 4. Intentar buscar en "gimbal_list"
                            if (dataElement.TryGetProperty("gimbal_list", out var gimbalList) && gimbalList.ValueKind == JsonValueKind.Array && gimbalList.GetArrayLength() > 0)
                            {
                                var firstGimbal = gimbalList[0];
                                if (gimbalPitch == -90.0)
                                {
                                    if (firstGimbal.TryGetProperty("pitch", out var gp5)) gimbalPitch = gp5.GetDouble();
                                    else if (firstGimbal.TryGetProperty("gimbal_pitch", out var gp6)) gimbalPitch = gp6.GetDouble();
                                }
                                if (gimbalRoll == 0.0)
                                {
                                    if (firstGimbal.TryGetProperty("roll", out var gr5)) gimbalRoll = gr5.GetDouble();
                                    else if (firstGimbal.TryGetProperty("gimbal_roll", out var gr6)) gimbalRoll = gr6.GetDouble();
                                }
                                if (gimbalYaw == 0.0)
                                {
                                    if (firstGimbal.TryGetProperty("yaw", out var gy5)) gimbalYaw = gy5.GetDouble();
                                    else if (firstGimbal.TryGetProperty("gimbal_yaw", out var gy6)) gimbalYaw = gy6.GetDouble();
                                }
                            }

                            // 5. Intentar buscar en "camera_list"
                            if (dataElement.TryGetProperty("camera_list", out var camList) && camList.ValueKind == JsonValueKind.Array && camList.GetArrayLength() > 0)
                            {
                                var firstCam = camList[0];
                                if (zoomFactor == 1.0)
                                {
                                    if (firstCam.TryGetProperty("zoomFactor", out var zf6)) zoomFactor = zf6.GetDouble();
                                    else if (firstCam.TryGetProperty("zoom_factor", out var zf7)) zoomFactor = zf7.GetDouble();
                                }
                            }

                            // 6. Buscar gimbal/zoom en claves de índice de cámara tipo "53-0-0", "67-0-0"
                            //    Formato DJI Cloud API para Mavic 3T, M30T, etc.
                            if (gimbalPitch == -90.0 || gimbalYaw == 0.0 || zoomFactor == 1.0)
                            {
                                foreach (var prop in dataElement.EnumerateObject())
                                {
                                    if (!System.Text.RegularExpressions.Regex.IsMatch(prop.Name, @"^\d+-\d+-\d+$") ||
                                        prop.Value.ValueKind != JsonValueKind.Object) continue;
                                    var cd = prop.Value;
                                    if (gimbalPitch == -90.0 && cd.TryGetProperty("gimbal_pitch", out var gp7)) gimbalPitch = gp7.GetDouble();
                                    if (gimbalRoll  == 0.0   && cd.TryGetProperty("gimbal_roll",  out var gr7)) gimbalRoll  = gr7.GetDouble();
                                    if (gimbalYaw   == 0.0   && cd.TryGetProperty("gimbal_yaw",   out var gy7)) gimbalYaw   = gy7.GetDouble();
                                    if (zoomFactor  == 1.0   && cd.TryGetProperty("zoom_factor",  out var zf8)) zoomFactor  = zf8.GetDouble();
                                    break; // primera cámara disponible
                                }
                            }

                            // Extraer satélites GPS
                            int gpsNumber = 0;
                            if (dataElement.TryGetProperty("gps_number", out var gpsProp))
                            {
                                if (gpsProp.ValueKind == JsonValueKind.Number && gpsProp.TryGetInt32(out var parsedGps))
                                {
                                    gpsNumber = parsedGps;
                                }
                                else if (gpsProp.ValueKind == JsonValueKind.String && int.TryParse(gpsProp.GetString(), out var parsedGpsStr))
                                {
                                    gpsNumber = parsedGpsStr;
                                }
                            }
                            else if (dataElement.TryGetProperty("position_state", out var posState2) && posState2.ValueKind == JsonValueKind.Object)
                            {
                                if (posState2.TryGetProperty("gps_number", out var gpsProp2))
                                {
                                    if (gpsProp2.ValueKind == JsonValueKind.Number && gpsProp2.TryGetInt32(out var parsedGps2))
                                    {
                                        gpsNumber = parsedGps2;
                                    }
                                    else if (gpsProp2.ValueKind == JsonValueKind.String && int.TryParse(gpsProp2.GetString(), out var parsedGpsStr2))
                                    {
                                        gpsNumber = parsedGpsStr2;
                                    }
                                }
                            }

                            // Extraer frecuencia SDR/4G
                            double sdrFreqBand = 0.0;
                            if (dataElement.TryGetProperty("sdr_freq_band", out var sdrProp))
                            {
                                if (sdrProp.ValueKind == JsonValueKind.Number && sdrProp.TryGetDouble(out var parsedSdr))
                                {
                                    sdrFreqBand = parsedSdr;
                                }
                                else if (sdrProp.ValueKind == JsonValueKind.String && double.TryParse(sdrProp.GetString(), out var parsedSdrStr))
                                {
                                    sdrFreqBand = parsedSdrStr;
                                }
                            }
                            else if (dataElement.TryGetProperty("wireless_link", out var wLink) && wLink.ValueKind == JsonValueKind.Object)
                            {
                                if (wLink.TryGetProperty("sdr_freq_band", out var sdrProp2))
                                {
                                    if (sdrProp2.ValueKind == JsonValueKind.Number && sdrProp2.TryGetDouble(out var parsedSdr2))
                                    {
                                        sdrFreqBand = parsedSdr2;
                                    }
                                    else if (sdrProp2.ValueKind == JsonValueKind.String && double.TryParse(sdrProp2.GetString(), out var parsedSdrStr2))
                                    {
                                        sdrFreqBand = parsedSdrStr2;
                                    }
                                }
                            }

                            // Determinar si el SN del topic es un gateway/RC (independiente del type code)
                            bool isGatewayOsd = adminData.IsGateway(sn);

                            var hubContext = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();

                            if (isGatewayOsd)
                            {
                                // El RC/Gateway no tiene GPS propio: relaya el OSD de la aeronave.
                                // Enviamos su posición bajo su PROPIO SN como evento separado para
                                // que el frontend lo muestre con icono de mando sin afectar al
                                // marcador de la aeronave (evita el salto errático).
                                await hubContext.Clients.All.SendAsync(
                                    "UpdateGatewayPosition", sn, latitude, longitude);

                                // TSA — como osdRemoteControl → pushOsdDataToPilot de la demo Java:
                                // device_osd del gateway hacia los pilotos con host reducido
                                // {latitude, longitude, height}. (gateway_osd es solo para web,
                                // que en nuestro caso va por SignalR.)
                                if (adminData.ShouldPushSignalR(sn + "_ws_gwosd", 1000))
                                {
                                    var gwOsdNotifier = app.Services.GetRequiredService<IMapSyncNotifier>();
                                    _ = gwOsdNotifier.NotifyGatewayOsdAsync(sn, latitude, longitude, altitude);
                                }
                            }
                            else
                            {
                                // Aeronave directa: posición real bajo su propio SN
                                string posSn = sn;

                                var trajectoryStore = app.Services.GetRequiredService<ITrajectoryStore>();
                                trajectoryStore.AddPosition(posSn, latitude, longitude, altitude);

                                await hubContext.Clients.All.SendCoreAsync("UpdateDronePosition",
                                    new object[] { posSn, latitude, longitude, altitude, gimbalPitch, gimbalRoll, gimbalYaw, heading, zoomFactor, batteryPercent, gpsNumber, sdrFreqBand, elevation });

                                var flightRecorder = app.Services.GetRequiredService<IFlightRecorderService>();
                                flightRecorder.AddFrame(posSn, latitude, longitude, altitude, heading, gimbalPitch, gimbalRoll, gimbalYaw, zoomFactor);
                            }

                            // Registrar la telemetría en el panel de administración
                            adminData.UpdateDeviceTelemetry(
                                sn, latitude, longitude, altitude, elevation, heading, gimbalPitch, gimbalRoll, gimbalYaw, zoomFactor,
                                batteryPercent, gpsNumber, sdrFreqBand,
                                modeCode, remainFlightTime, horizontalSpeed, verticalSpeed,
                                attitudePitch, attitudeRoll, gpsFixed > 0);

                            // TSA (Situational Awareness) — spec DJI doc 58: device_osd WS push
                            // El servidor reenvía la posición de cada aeronave al RC vía WS a 1 Hz.
                            // El RC muestra el icono del dron moviéndose en su propio mapa de situación.
                            // Solo para aeronaves reales (no RC/Gateway): isGatewayOsd == false.
                            if (!isGatewayOsd && adminData.ShouldPushSignalR(effectiveSn + "_ws_osd", 1000))
                            {
                                var osdNotifier = app.Services.GetRequiredService<IMapSyncNotifier>();
                                _ = osdNotifier.NotifyDeviceOsdAsync(
                                    effectiveSn,
                                    latitude, longitude, altitude,
                                    heading, elevation,
                                    horizontalSpeed, verticalSpeed);
                            }
                        }
                        skipPosition:; // destino del goto cuando lat/lon = (0,0)

                        // ── Estado de cámaras (grabación, zoom, foto) ────────────────────────
                        // La API publica "cameras" en thing/product/{sn}/state cuando hay cambio
                        if (dataElement.TryGetProperty("cameras", out var camsEl) && camsEl.ValueKind == JsonValueKind.Array)
                        {
                            var camStates = new List<object>();
                            foreach (var cam in camsEl.EnumerateArray())
                            {
                                var cs = new System.Collections.Generic.Dictionary<string, object>();
                                if (cam.TryGetProperty("camera_index",    out var ci2)) cs["cameraIndex"]    = ci2.GetString() ?? "";
                                if (cam.TryGetProperty("recording_state", out var rs))  cs["recordingState"] = rs.ValueKind == JsonValueKind.Number ? (object)rs.GetInt32() : rs.GetString() ?? "0";
                                if (cam.TryGetProperty("photo_state",     out var ps))  cs["photoState"]     = ps.ValueKind == JsonValueKind.Number ? (object)ps.GetInt32() : ps.GetString() ?? "0";
                                if (cam.TryGetProperty("zoom_factor",     out var zfc)) cs["zoomFactor"]     = zfc.GetDouble();
                                if (cam.TryGetProperty("ir_switch",       out var irs)) cs["irSwitch"]       = irs.ValueKind == JsonValueKind.True;
                                if (cam.TryGetProperty("screen_type",     out var st2)) cs["screenType"]     = st2.GetString() ?? "";
                                camStates.Add(cs);
                            }
                            if (camStates.Count > 0)
                            {
                                var hubCam = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                                await hubCam.Clients.All.SendAsync("UpdateCameraState", effectiveSn, camStates);
                            }
                        }

                        // Parsear live_capacity si viene en este mensaje de estado
                        if (dataElement.TryGetProperty("live_capacity", out var lcEl))
                        {
                            var cap = ParseLiveCapacity(sn, lcEl);
                            if (cap != null) adminData.SetLiveCapacity(sn, cap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Evitamos que un error de parsing bloquee el broker
                    app.Logger.LogWarning(ex, "[MQTT] Error al procesar telemetría. SN={Sn}", sn);
                }
            }
        }
    };
});

// Crear directorios estáticos necesarios en wwwroot
var wwwroot = app.Environment.WebRootPath;
foreach (var dir in new[] { "hls", "videos", "klv", "missions", "routes", "flights", "mqtt_logs", "media", "waylines", "pilot_logs" })
    Directory.CreateDirectory(Path.Combine(wwwroot, dir));

// Detener ffmpeg limpiamente al apagar la aplicación
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
    app.Services.GetRequiredService<IFfmpegService>().Stop());

// ── TSA: detección de offline por inactividad (cada 30s) ─────────────────────
// Equivalente al GlobalScheduleService de la demo Java (TTL Redis + tarea programada).
// Los drones que dejan de emitir OSD >15s se marcan offline (cutoff en GetDevices)
// y se notifica device_offline a los RC conectados para que actualicen su mapa TSA.
_ = Task.Run(async () =>
{
    var lastOnlineSns = new HashSet<string>();
    using var offlineTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    try
    {
        while (await offlineTimer.WaitForNextTickAsync(lifetime.ApplicationStopping))
        {
            try
            {
                var adminSvc = app.Services.GetRequiredService<IAdminDataService>();
                var tsaSvc   = app.Services.GetRequiredService<IMapSyncNotifier>();
                // GetDevices aplica el cutoff de inactividad y marca offline
                var nowOnline = adminSvc.GetDevices()
                    .Where(d => d.IsOnline && d.DeviceType != "Cliente MQTT")
                    .Select(d => d.Sn)
                    .ToHashSet();

                foreach (var sn in lastOnlineSns.Except(nowOnline))
                {
                    _ = tsaSvc.NotifyDeviceOfflineAsync(sn);
                    app.Logger.LogInformation("[TSA] device_offline por inactividad → SN={Sn}", sn);
                }
                lastOnlineSns = nowOnline;
            }
            catch (Exception ex)
            {
                app.Logger.LogDebug(ex, "[TSA] Error en chequeo periódico de offline");
            }
        }
    }
    catch (OperationCanceledException) { /* apagado de la aplicación */ }
});

// Archivos estáticos con MIME types explícitos para HLS
var mimeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
mimeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
mimeProvider.Mappings[".ts"]   = "video/mp2t";          // MPEG-TS (HLS segments)
mimeProvider.Mappings[".klv"]  = "application/octet-stream";
mimeProvider.Mappings[".kmz"]  = "application/vnd.google-earth.kmz"; // misiones WPML
mimeProvider.Mappings[".wpml"] = "application/xml";

// Middleware para registrar las solicitudes HTTP a la API (DJI Cloud API)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    var method = context.Request.Method;
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ── Capturar body para POST/PUT en paths de mapa ──────────────────────────
    // Permite diagnosticar qué envía el RC cuando crea elementos (debug de "toto").
    // Solo para peticiones del RC (no localhost) y métodos con body.
    string? requestBodySnippet = null;
    if ((method == "POST" || method == "PUT") &&
        (path.StartsWith("/map/api/") || path.StartsWith("/api/")) &&
        !ip.Contains("::1") && !ip.Contains("127.0.0.1"))
    {
        context.Request.EnableBuffering();
        try
        {
            var bodyReader = new System.IO.StreamReader(context.Request.Body, leaveOpen: true);
            var fullBody = await bodyReader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            if (fullBody.Length > 0)
                requestBodySnippet = fullBody.Length > 300 ? fullBody[..300] + "…" : fullBody;
        }
        catch { /* no crítico */ }
    }

    // Skip admin panel paths from being logged (too noisy)
    bool skipLog = path.StartsWith("/api/admin") || path.StartsWith("/signalr") ||
                   path.StartsWith("/_blazor") || path == "/favicon.ico" ||
                   path.StartsWith("/lib/") || path.EndsWith(".js") || path.EndsWith(".css");

    if (skipLog)
    {
        await next(context);
        return;
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await next(context);
    }
    finally
    {
        stopwatch.Stop();
        var statusCode = context.Response.StatusCode;
        var elapsed = stopwatch.ElapsedMilliseconds;

        var adminData = context.RequestServices.GetRequiredService<IAdminDataService>();
        adminData.RecordRequest(method, path, statusCode, elapsed, ip);

        // Si el RC envió un body en un POST/PUT, loguearlo para diagnóstico
        if (requestBodySnippet != null)
        {
            // Solo loguear si parece interesante (contiene "name", "element", "toto", etc.)
            bool isInteresting = statusCode == 404 ||
                                 requestBodySnippet.Contains("toto", StringComparison.OrdinalIgnoreCase) ||
                                 requestBodySnippet.Contains("name") ||
                                 path.Contains("element");
            if (isInteresting)
            {
                adminData.AddLog("INFO", "HTTP-Body",
                    $"{method} {path} [{statusCode}] body={requestBodySnippet}");
                app.Logger.LogInformation("[HTTP-Body] {Method} {Path} [{Status}] body={Body}",
                    method, path, statusCode, requestBodySnippet);
            }
        }

        // Loguear 404s del RC para detectar endpoints no implementados
        if (statusCode == 404 && !ip.Contains("::1"))
        {
            adminData.AddLog("WARN", "HTTP-404",
                $"RC llamó a endpoint no implementado: {method} {path}");
            app.Logger.LogWarning("[HTTP-404] {Method} {Path} desde {IP}", method, path, ip);
        }
    }
});

// Habilitar soporte de WebSockets crudos para el mando DJI Pilot 2
// KeepAliveInterval envía pings automáticos para evitar que NAT/routers corten la conexión silenciosamente
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            // Si el bloqueo está activo, rechazar la conexión para mantener el RC offline.
            // Esto permite al usuario crear elementos mientras Pilot 2 cree que no hay servidor.
            var wsManagerCheck = context.RequestServices.GetRequiredService<IDjiWebSocketManager>();
            if (wsManagerCheck.BlockNewConnections)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                app.Logger.LogInformation("[WebSocket] Conexión rechazada — WS bloqueado (admin/block-ws activo)");
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            app.Logger.LogInformation("[WebSocket] Mando conectado.");

            var adminData = context.RequestServices.GetRequiredService<IAdminDataService>();
            adminData.AddLog("INFO", "WebSocket", "Mando conectado al canal WebSocket (/ws)");

            var wsManager = context.RequestServices.GetRequiredService<IDjiWebSocketManager>();
            // Scoping multi-workspace: workspace de la query (?workspace_id=) o el del despliegue
            var wsWorkspaceId = context.Request.Query["workspace_id"].FirstOrDefault()
                ?? context.RequestServices.GetRequiredService<IOptions<DjiCloudOptions>>().Value.WorkspaceId;
            wsManager.Add(webSocket, wsWorkspaceId);

            // Notificar al mapa web que el mando RC ha conectado su WebSocket
            try
            {
                var wsHub = context.RequestServices.GetRequiredService<IHubContext<TelemetryHub>>();
                _ = wsHub.Clients.All.SendAsync("RcWsConnected");
            }
            catch { /* no crítico */ }

            // Enviar map_group_refresh al reconectarse con un breve delay:
            // – El delay (800ms) permite que el WS termine de inicializarse antes de recibir datos.
            //   Sin él, el RC cierra el WS inmediatamente (race condition de 4ms).
            // – Solo enviamos el grupo App Shared (zero-UUID), que es el único que DJI Pilot 2
            //   conoce. Enviar el grupo Web causaba que Pilot 2 lo desconociera y entrara en
            //   estado de error, cerrando el WS.
            _ = Task.Run(async () =>
            {
                try
                {
                    // #2.9: cancelable en el shutdown de la aplicación
                    await Task.Delay(800, app.Lifetime.ApplicationStopping);
                    var mapNotifier = context.RequestServices.GetRequiredService<IMapSyncNotifier>();
                    // Al conectar el mando, refrescar TODOS los grupos reales para que re-descargue
                    // los elementos del servidor y dibuje los que le falten (primera conexión incluida).
                    var mapData  = context.RequestServices.GetRequiredService<IMapDataService>();
                    var groupIds = mapData.GetGroups().Select(g => g.Id).ToArray();
                    await mapNotifier.NotifyGroupRefreshAsync(groupIds);
                    app.Logger.LogInformation("[WebSocket] map_group_refresh ({Count} grupos) enviado al conectar mando.", groupIds.Length);
                }
                catch (OperationCanceledException)
                {
                    // Apagado durante el delay — no es un error
                }
                catch (Exception ex)
                {
                    app.Logger.LogWarning(ex, "[WebSocket] No se pudo enviar map_group_refresh al reconectar.");
                }
            });

            // Buffer grande para aceptar payloads completos (elementos GeoJSON pueden ser varios KB)
            var buffer = new byte[65536];
            try
            {
                while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    // Acumular frames hasta completar el mensaje completo
                    var ms = new System.IO.MemoryStream();
                    System.Net.WebSockets.WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Cierre solicitado", CancellationToken.None);
                        break;
                    }

                    if (ms.Length == 0) continue;
                    var msg = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                    // ── Heartbeat "ping" → "pong" ──────────────────────────────────────────
                    // DJI Pilot 2 envía "ping" (texto plano) cada ~3s como health-check del
                    // canal. La demo Java NO responde, pero FlightHub (donde la sincronización
                    // en tiempo real SÍ funciona) no es la demo: si Pilot no recibe pong puede
                    // marcar el canal como degradado y suprimir el envío en vivo de elementos.
                    // NOTA: todas las pruebas previas de creación en vivo se hicieron sin pong
                    // activo — esta respuesta está bajo prueba como habilitador del RT-sync.
                    if (msg == "ping")
                    {
                        var pongBytes = System.Text.Encoding.UTF8.GetBytes("pong");
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(pongBytes),
                            System.Net.WebSockets.WebSocketMessageType.Text,
                            endOfMessage: true,
                            CancellationToken.None);
                        continue;
                    }

                    // ── Parsear mensajes JSON entrantes del mando ──────────────────────────
                    // DJI Pilot 2 puede enviar eventos de mapa via WS en lugar de REST cuando
                    // está conectado. Si ignoramos estos mensajes, los elementos se marcan como
                    // "ya sincronizados" en el RC y no vuelven a aparecer en el batch REST.
                    try
                    {
                        var json = Newtonsoft.Json.Linq.JObject.Parse(msg);
                        var bizCode = json["biz_code"]?.ToString() ?? json["method"]?.ToString() ?? "";

                        app.Logger.LogInformation("[WebSocket] Mensaje RC biz_code='{BizCode}' payload={Payload}",
                            bizCode, msg.Length > 500 ? msg[..500] + "…" : msg);
                        adminData.AddLog("INFO", "WebSocket", $"Mensaje RC: biz_code={bizCode}");

                        // Gestión de elementos de mapa enviados via WS por el RC
                        if (bizCode == "map_element_create" || bizCode == "map_element_update")
                        {
                            var mapData = context.RequestServices.GetRequiredService<IMapDataService>();
                            var notifier = context.RequestServices.GetRequiredService<IMapSyncNotifier>();

                            // El payload puede ser un solo elemento o un array bajo "data"
                            var dataNode = json["data"];
                            var elements = dataNode is Newtonsoft.Json.Linq.JArray arr
                                ? arr.Cast<Newtonsoft.Json.Linq.JObject>()
                                : new[] { dataNode as Newtonsoft.Json.Linq.JObject }.Where(x => x != null);

                            foreach (var elemJson in elements)
                            {
                                if (elemJson == null) continue;
                                var elName = elemJson["name"]?.ToString() ?? "";
                                // Extraer operador del nombre automático del RC (p.ej. "local_user 50" → "local_user")
                                string? elUser = null;
                                if (!string.IsNullOrEmpty(elName))
                                {
                                    var elLastSpace = elName.LastIndexOf(' ');
                                    elUser = elLastSpace > 0 && int.TryParse(elName[(elLastSpace + 1)..], out _)
                                        ? elName[..elLastSpace] : elName;
                                }
                                var elResource = elemJson["resource"] as Newtonsoft.Json.Linq.JObject;
                                // user_name persistido en el resource — alineado con la demo Java
                                if (elResource != null && string.IsNullOrEmpty(elResource["user_name"]?.ToString()))
                                    elResource["user_name"] = elUser ?? "pilot";

                                var el = new DjiCloudServer.Models.MapElement
                                {
                                    Id       = elemJson["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                                    Name     = elName,
                                    UserName = elUser,
                                    Resource = elResource
                                };
                                var groupId = elemJson["group_id"]?.ToString() ?? "00000000-0000-0000-0000-000000000000";
                                var saved = mapData.AddElement(groupId, el);
                                app.Logger.LogInformation("[WebSocket] Elemento guardado via WS: id={Id} nombre='{Name}' grupo={Group}",
                                    saved.Id, saved.Name, groupId);
                                _ = notifier.NotifyCreateAsync(saved);
                                // Pilot 2 v17 ignora map_element_create — refresh para que otros RC re-descarguen
                                // (#1.7: configurable via DjiCloud:LegacyGroupRefresh)
                                if (context.RequestServices.GetRequiredService<IOptions<DjiCloudOptions>>().Value.LegacyGroupRefresh)
                                    _ = notifier.NotifyGroupRefreshAsync(new[] { groupId });
                            }
                        }
                        else if (bizCode == "map_element_delete")
                        {
                            var mapData = context.RequestServices.GetRequiredService<IMapDataService>();
                            var notifier = context.RequestServices.GetRequiredService<IMapSyncNotifier>();
                            var dataNode = json["data"];
                            var ids = dataNode is Newtonsoft.Json.Linq.JArray idArr
                                ? idArr.Select(x => x.ToString())
                                : new[] { dataNode?["id"]?.ToString() }.Where(x => x != null);

                            foreach (var id in ids)
                            {
                                if (string.IsNullOrEmpty(id)) continue;
                                var el = mapData.GetElement(id);
                                var gid = el?.GroupId ?? "";
                                mapData.DeleteElement(id);
                                _ = notifier.NotifyDeleteAsync(id, gid);
                                app.Logger.LogInformation("[WebSocket] Elemento eliminado via WS: id={Id}", id);
                            }
                        }
                        else
                        {
                            // Heartbeat u otros mensajes — solo loguear
                            app.Logger.LogDebug("[WebSocket] Mensaje no gestionado biz_code='{BizCode}'", bizCode);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        // No es JSON o formato inesperado — loguear raw
                        app.Logger.LogWarning("[WebSocket] Mensaje no parseble ({Len} bytes): {Raw}",
                            ms.Length, msg.Length > 200 ? msg[..200] : msg);
                        adminData.AddLog("WARN", "WebSocket", $"Mensaje no parseble: {(msg.Length > 100 ? msg[..100] : msg)}");
                    }
                }
            }
            catch (System.Net.WebSockets.WebSocketException)
            {
                // Desconexión abrupta — no es un error real
            }
            finally
            {
                wsManager.Remove(webSocket);
                app.Logger.LogInformation("[WebSocket] Mando desconectado.");
                adminData.AddLog("WARN", "WebSocket", "Mando desconectado del canal WebSocket");

                // Notificar al mapa web que el mando RC ha perdido su WebSocket
                try
                {
                    var wsHub = context.RequestServices.GetRequiredService<IHubContext<TelemetryHub>>();
                    _ = wsHub.Clients.All.SendAsync("RcWsDisconnected");
                }
                catch { /* no crítico */ }
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider   = mimeProvider,
    ServeUnknownFileTypes = true,    // servir cualquier extensión desconocida
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});

app.UseCors("DjiPolicy");

app.UseAuthorization();

// ─── Helper: parsear live_capacity desde un JsonElement ──────────────────────
static LiveCapacityDto? ParseLiveCapacity(string gatewaySn, JsonElement el)
{
    if (!el.TryGetProperty("device_list", out var deviceList) ||
        deviceList.ValueKind != JsonValueKind.Array) return null;

    var dto = new LiveCapacityDto { GatewaySn = gatewaySn };
    foreach (var dev in deviceList.EnumerateArray())
    {
        var devDto = new LiveDeviceDto();
        if (dev.TryGetProperty("sn", out var snEl)) devDto.Sn = snEl.GetString() ?? "";
        if (dev.TryGetProperty("camera_list", out var camList) &&
            camList.ValueKind == JsonValueKind.Array)
        {
            foreach (var cam in camList.EnumerateArray())
            {
                var camDto = new LiveCameraDto();
                if (cam.TryGetProperty("camera_index", out var ciEl))
                    camDto.CameraIndex = ciEl.GetString() ?? "";
                if (cam.TryGetProperty("video_list", out var vidList) &&
                    vidList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var vid in vidList.EnumerateArray())
                    {
                        var vidDto = new LiveVideoDto();
                        if (vid.TryGetProperty("video_index", out var viEl)) vidDto.VideoIndex = viEl.GetString() ?? "";
                        if (vid.TryGetProperty("status",      out var stEl)) vidDto.Status     = stEl.GetInt32();
                        camDto.VideoList.Add(vidDto);
                    }
                }
                devDto.CameraList.Add(camDto);
            }
        }
        dto.DeviceList.Add(devDto);
    }
    return dto.DeviceList.Count > 0 ? dto : null;
}

app.MapControllers();
app.MapHub<TelemetryHub>("/telemetryHub");

// Ruta raíz → Swagger en desarrollo
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// ─── Network probe periódico ──────────────────────────────────────────────
// DJI Pilot 2 requiere recibir probes de red del servidor antes de autorizar
// el live streaming. Sin probes, ignora los comandos live_start_push.
//
// El mismo bucle envía el heartbeat DRC (thing/product/{sn}/drc/down) cada 3 s
// con un seq incremental por gateway — DJI cierra el canal si no recibe el heartbeat.
var _drcHeartbeatSeq = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
var _probeAppCt = app.Lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    // Esperar a que el servidor esté completamente iniciado
    try { await Task.Delay(3000, _probeAppCt); }
    catch (OperationCanceledException) { return; }

    while (!_probeAppCt.IsCancellationRequested)
    {
        try
        {
            var mqttSvc   = app.Services.GetRequiredService<IMqttService>();
            var adminSvc  = app.Services.GetRequiredService<IAdminDataService>();
            var ts        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Envelope común DJI (doc 27): tid, bid, timestamp, data — obligatorio en todos los mensajes
            var probeJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                tid       = Guid.NewGuid().ToString(),
                bid       = Guid.NewGuid().ToString(),
                timestamp = ts,
                data      = new { }
            });

            foreach (var gw in adminSvc.GetGateways().Where(g => g.IsOnline))
            {
                // Probe de red SOLO al gateway: sys/product/{sn}/... es dominio del gateway
                // (el RC solo se suscribe a su propio SN — la aeronave nunca lo recibe).
                await mqttSvc.PublishAsync($"sys/product/{gw.GatewaySn}/network/probe",
                    probeJson, MqttQualityOfServiceLevel.AtMostOnce);

                // Heartbeat DRC — obligatorio cada ≤3 s para que DJI mantenga el canal abierto.
                // Formato spec doc 32 (DRC-Heartbeat): seq en la RAÍZ (mismo nivel que data),
                // timestamp dentro de data:  { "data": {"timestamp": ...}, "method": "heart_beat", "seq": N }
                var seq = _drcHeartbeatSeq.AddOrUpdate(gw.GatewaySn, 1, (_, v) => v + 1);
                var hbPayload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    data   = new { timestamp = ts },
                    method = "heart_beat",
                    seq
                });
                try
                {
                    await mqttSvc.PublishAsync($"thing/product/{gw.GatewaySn}/drc/down",
                        hbPayload, MqttQualityOfServiceLevel.AtMostOnce);
                }
                catch { /* ignorar si el canal DRC no está activo */ }
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogDebug(ex, "[Probe] Error en bucle de probes/heartbeat DRC");
        }

        try { await Task.Delay(3000, _probeAppCt); }
        catch (OperationCanceledException) { return; }
    }
});

// ─── Emisor TSA periódico (conciencia situacional ENTRE mandos) ─────────────
// Difunde device_osd de TODOS los dispositivos online con posición conocida
// (aeronaves + mandos) a todos los mandos del workspace, cada 2 s.
// CLAVE: desacoplado del OSD instantáneo. La posición de los MANDOS no se compartía
// porque su propio OSD reporta lat/lon=0; aquí usamos la última posición conocida del
// store, así cada mando ve en su mapa de situación a los demás mandos y aeronaves.
var _tsaAppCt = app.Lifetime.ApplicationStopping;
_ = Task.Run(async () =>
{
    try { await Task.Delay(4000, _tsaAppCt); }
    catch (OperationCanceledException) { return; }

    while (!_tsaAppCt.IsCancellationRequested)
    {
        try
        {
            var adminSvc = app.Services.GetRequiredService<IAdminDataService>();
            var tsaNotif = app.Services.GetRequiredService<IMapSyncNotifier>();

            foreach (var dev in adminSvc.GetDevices())
            {
                if (!dev.IsOnline) continue;
                if (dev.DeviceType is "Cliente MQTT") continue;
                // Solo difundir si hay posición real (lat/lon != 0,0)
                if (dev.Latitude == 0.0 && dev.Longitude == 0.0) continue;

                await tsaNotif.NotifyDeviceOsdAsync(
                    dev.Sn,
                    dev.Latitude, dev.Longitude, dev.Altitude,
                    dev.Heading, dev.Elevation,
                    dev.HorizontalSpeed, dev.VerticalSpeed);
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogDebug(ex, "[TSA] Error en bucle de difusión de posiciones");
        }

        try { await Task.Delay(2000, _tsaAppCt); }
        catch (OperationCanceledException) { return; }
    }
});

app.Run();

// ─── Funciones locales ────────────────────────────────────────────────────────

static void OpenRtmpFirewallRule(ILogger logger)
{
    try
    {
        var rule = "DjiCloudServer-RTMP";
        var args = $"advfirewall firewall show rule name=\"{rule}\"";
        var check = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", args)
        {
            RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
        });
        check?.WaitForExit(3000);
        if (check?.ExitCode == 0) return; // regla ya existe

        var addArgs = $"advfirewall firewall add rule name=\"{rule}\" dir=in action=allow protocol=TCP localport=1935-1965 profile=any";
        var add = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", addArgs)
        {
            RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
        });
        add?.WaitForExit(5000);
        logger.LogInformation("[Firewall] Regla RTMP creada (puertos 1935-1965 TCP)");
    }
    catch (Exception ex)
    {
        logger.LogDebug(ex, "[Firewall] No se pudo abrir la regla RTMP (sin permisos de administrador)");
    }
}
