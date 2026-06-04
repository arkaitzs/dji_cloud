using DjiCloudServer;
using DjiCloudServer.Services;
using DjiCloudServer.Hubs;
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

        // ── Parsear eventos HMS (Health Management System) ────────────────────
        if (topic.StartsWith("thing/product/") && topic.EndsWith("/events"))
        {
            var snEvt = topic.Split('/').ElementAtOrDefault(2) ?? "";
            try
            {
                using var evDoc = JsonDocument.Parse(payload);
                var evRoot = evDoc.RootElement;
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
                                    await Task.Delay(800);
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
                var devTypeForCap = adminData.GetDeviceTypeCode(sn);
                if (devTypeForCap != 144) // Si es aeronave, buscar su RC/Gateway
                {
                    gatewaySnForCap = adminData.GetAircraftForGateway(sn) ?? 
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
                            _ = Task.Run(async () =>
                            {
                                try
                                {
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

                        // Guardar tipo de dispositivo (144 = Mando RC, resto = aeronave) y emparejamientos
                        if (root.TryGetProperty("data", out var topoData))
                        {
                            if (topoData.TryGetProperty("type", out var typeEl) &&
                                typeEl.ValueKind == JsonValueKind.Number)
                            {
                                adminData.SetDeviceTypeCode(sn, typeEl.GetInt32());
                                app.Logger.LogDebug("[MQTT] Dispositivo registrado. SN={Sn} Tipo={Type} ({Role})", sn, typeEl.GetInt32(), typeEl.GetInt32() == 144 ? "Mando RC" : "Aeronave");
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
                                            if (subDev.TryGetProperty("type", out var subTypeVal) &&
                                                subTypeVal.ValueKind == JsonValueKind.Number)
                                            {
                                                adminData.SetDeviceTypeCode(subSn, subTypeVal.GetInt32());
                                            }
                                            if (subDev.TryGetProperty("sub_type", out var subSubTypeVal) &&
                                                subSubTypeVal.ValueKind == JsonValueKind.Number)
                                            {
                                                adminData.SetDeviceSubtypeCode(subSn, subSubTypeVal.GetInt32());
                                            }
                                            adminData.SetRcAircraftPairing(sn, subSn);
                                            app.Logger.LogDebug("[MQTT] Emparejamiento RC↔aeronave. Gateway={GwSn} Aircraft={AcSn}", sn, subSn);
                                        }
                                    }
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
                        int gpsFixed = 0, gpsCountStatus = 0;
                        if (dataElement.TryGetProperty("position_state", out var posState) && posState.ValueKind == JsonValueKind.Object)
                        {
                            if (posState.TryGetProperty("is_fixed",   out var isFixed)) gpsFixed        = isFixed.ValueKind == JsonValueKind.Number ? isFixed.GetInt32() : 0;
                            if (posState.TryGetProperty("gps_number", out var gpsNs))   gpsCountStatus   = gpsNs.ValueKind   == JsonValueKind.Number ? gpsNs.GetInt32()  : 0;
                        }
                        if (dataElement.TryGetProperty("gps_number", out var gpsDirectN) && gpsDirectN.ValueKind == JsonValueKind.Number)
                            gpsCountStatus = gpsDirectN.GetInt32();

                        // Velocidad horizontal y rumbo de aeronave (sin GPS)
                        double horizontalSpeed = 0.0, attitudeHead = 0.0;
                        if (dataElement.TryGetProperty("horizontal_speed", out var hs)) horizontalSpeed = hs.GetDouble();
                        if (dataElement.TryGetProperty("attitude_head",    out var ah)) attitudeHead    = ah.GetDouble();
                        else if (dataElement.TryGetProperty("yaw",    out var yawS))   attitudeHead    = yawS.GetDouble();
                        else if (dataElement.TryGetProperty("heading", out var hdgS))  attitudeHead    = hdgS.GetDouble();

                        // ── Mapeo RC↔aeronave desde thing/{sn}/state con campo "gateway" ──────
                        if (root.TryGetProperty("gateway", out var gwEl) && gwEl.ValueKind == JsonValueKind.String)
                        {
                            var rcSn = gwEl.GetString()!;
                            if (!string.IsNullOrEmpty(rcSn) && rcSn != sn)
                            {
                                adminData.SetRcAircraftPairing(rcSn, sn);
                            }
                        }

                        // Tipo de dispositivo: 144=Mando RC, -1=desconocido, resto=aeronave
                        int devType = adminData.GetDeviceTypeCode(sn);

                        // Si es el RC y hay una aeronave pareada, usamos el SN de la aeronave
                        // para que el estado aparezca bajo el SN correcto en el mapa
                        string effectiveSn = sn;
                        var pairedAircraft = adminData.GetAircraftForGateway(sn);
                        if (devType == 144 && pairedAircraft is not null)
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
                        int sendDevType = (effectiveSn != sn) ? -2 : devType;
                        var hubContextStatus = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();
                        await hubContextStatus.Clients.All.SendAsync("UpdateDroneStatus",
                            effectiveSn, batteryPercent, remainFlightTime, modeCode,
                            gpsFixed, gpsCountStatus, attitudeHead, horizontalSpeed, sendDevType);

                        // Refrescar LastSeen y batería del RC/gateway (no manda lat/lon propio)
                        if (devType == 144)
                            adminData.RefreshDeviceStatus(sn, batteryPercent, modeCode);

                        // ── POSICIÓN — solo cuando hay fix GPS válido ─────────────────────────
                        if (dataElement.TryGetProperty("latitude", out var lat) &&
                            dataElement.TryGetProperty("longitude", out var lon))
                        {
                            double latitude  = lat.GetDouble();
                            double longitude = lon.GetDouble();

                            // Coordenadas (0,0) = sin fix real — ignorar
                            if (latitude == 0.0 && longitude == 0.0) goto skipPosition;

                            double altitude = dataElement.TryGetProperty("height", out var heightProp) ? heightProp.GetDouble() : 0.0;
                            
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

                            // Determinar si el SN del topic es un gateway/RC (devType=144)
                            int posDevType = adminData.GetDeviceTypeCode(sn);
                            bool isGatewayOsd = (posDevType == 144);

                            var hubContext = app.Services.GetRequiredService<IHubContext<TelemetryHub>>();

                            if (isGatewayOsd)
                            {
                                // El RC/Gateway no tiene GPS propio: relaya el OSD de la aeronave.
                                // Enviamos su posición bajo su PROPIO SN como evento separado para
                                // que el frontend lo muestre con icono de mando sin afectar al
                                // marcador de la aeronave (evita el salto errático).
                                await hubContext.Clients.All.SendAsync(
                                    "UpdateGatewayPosition", sn, latitude, longitude);
                            }
                            else
                            {
                                // Aeronave directa: posición real bajo su propio SN
                                string posSn = sn;

                                var trajectoryStore = app.Services.GetRequiredService<ITrajectoryStore>();
                                trajectoryStore.AddPosition(posSn, latitude, longitude, altitude);

                                await hubContext.Clients.All.SendCoreAsync("UpdateDronePosition",
                                    new object[] { posSn, latitude, longitude, altitude, gimbalPitch, gimbalRoll, gimbalYaw, heading, zoomFactor, batteryPercent, gpsNumber, sdrFreqBand });

                                var flightRecorder = app.Services.GetRequiredService<IFlightRecorderService>();
                                flightRecorder.AddFrame(posSn, latitude, longitude, altitude, heading, gimbalPitch, gimbalRoll, gimbalYaw, zoomFactor);
                            }

                            // Registrar la telemetría en el panel de administración
                            adminData.UpdateDeviceTelemetry(sn, latitude, longitude, altitude, heading, gimbalPitch, gimbalRoll, gimbalYaw, zoomFactor, batteryPercent, gpsNumber, sdrFreqBand);
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
foreach (var dir in new[] { "hls", "videos", "klv", "missions", "routes", "flights", "mqtt_logs" })
    Directory.CreateDirectory(Path.Combine(wwwroot, dir));

// Detener ffmpeg limpiamente al apagar la aplicación
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
    app.Services.GetRequiredService<IFfmpegService>().Stop());

// Archivos estáticos con MIME types explícitos para HLS
var mimeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
mimeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
mimeProvider.Mappings[".ts"]   = "video/mp2t";          // MPEG-TS (HLS segments)
mimeProvider.Mappings[".klv"]  = "application/octet-stream";

// Middleware para registrar las solicitudes HTTP a la API (DJI Cloud API)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (!path.StartsWith("/api/") || path.StartsWith("/api/admin"))
    {
        await next(context);
        return;
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var method = context.Request.Method;

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
    }
});

// Habilitar soporte de WebSockets crudos para el mando DJI Pilot 2
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            app.Logger.LogInformation("[WebSocket] Mando conectado.");
            
            var adminData = context.RequestServices.GetRequiredService<IAdminDataService>();
            adminData.AddLog("INFO", "WebSocket", "Mando conectado al canal WebSocket (/ws)");
            
            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Cierre solicitado", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (System.Net.WebSockets.WebSocketException)
            {
                // Desconexión abrupta del cliente (cierre de pestaña, pérdida de red), no es un error real
            }
            finally
            {
                app.Logger.LogInformation("[WebSocket] Mando desconectado.");
                adminData.AddLog("WARN", "WebSocket", "Mando desconectado del canal WebSocket");
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
            var probeJson = System.Text.Json.JsonSerializer.Serialize(new { timestamp = ts });

            foreach (var gw in adminSvc.GetGateways().Where(g => g.IsOnline))
            {
                // Probe de red al gateway
                await mqttSvc.PublishAsync($"sys/product/{gw.GatewaySn}/network/probe",
                    probeJson, MqttQualityOfServiceLevel.AtMostOnce);

                // Probe de red a la aeronave pareada (si existe)
                if (!string.IsNullOrEmpty(gw.AircraftSn))
                    await mqttSvc.PublishAsync($"sys/product/{gw.AircraftSn}/network/probe",
                        probeJson, MqttQualityOfServiceLevel.AtMostOnce);

                // Heartbeat DRC — obligatorio cada ≤3 s para que DJI mantenga el canal abierto.
                // El campo 'seq' debe incrementarse en cada envío (spec DJI Cloud API).
                if (adminSvc.IsDrcActive(gw.GatewaySn))
                {
                    var seq = _drcHeartbeatSeq.AddOrUpdate(
                        gw.GatewaySn,
                        addValue:    1,
                        updateValueFactory: (_, prev) => prev + 1);

                    var hbJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        method = "heart_beat",
                        data   = new { seq, timestamp = ts }
                    });
                    await mqttSvc.PublishAsync($"thing/product/{gw.GatewaySn}/drc/down",
                        hbJson, MqttQualityOfServiceLevel.AtMostOnce);
                }
                else
                {
                    // Si el DRC se desactivó, resetear el seq para la próxima sesión
                    _drcHeartbeatSeq.TryRemove(gw.GatewaySn, out _);
                }
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "[MQTT-Probe] Error en ciclo de probes/heartbeat. El bucle continúa.");
        }

        try { await Task.Delay(3000, _probeAppCt); }
        catch (OperationCanceledException) { break; }
    }

    app.Logger.LogInformation("[MQTT-Probe] Bucle de probes detenido (app shutdown).");
});

app.Run();

// ─── Helpers de arranque ──────────────────────────────────────────────────────

static void OpenRtmpFirewallRule(ILogger logger)
{
    try
    {
        // Abrir el rango completo del pool RTMP (1935-1954) para entrada TCP
        const string args = "advfirewall firewall add rule " +
                            "name=\"DJI_Cloud_RTMP\" " +
                            "dir=in action=allow protocol=TCP " +
                            "localport=1935-1954 enable=yes";
        using var proc = System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo("netsh", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            })!;
        proc.WaitForExit(5000);
        var output = proc.StandardOutput.ReadToEnd().Trim();
        if (proc.ExitCode == 0)
            logger.LogInformation("[Firewall] Regla DJI_Cloud_RTMP (1935-1954) creada/confirmada: {Output}", output);
        else
            logger.LogWarning("[Firewall] netsh salió con código {Code}: {Output}", proc.ExitCode, output);
    }
    catch (Exception ex)
    {
        logger.LogWarning("[Firewall] No se pudo aplicar la regla (¿sin permisos de administrador?): {Msg}", ex.Message);
    }
}
