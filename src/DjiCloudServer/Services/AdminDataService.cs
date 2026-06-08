using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;

namespace DjiCloudServer.Services;

public class SystemStatusDto
{
    public string Status { get; set; } = "OK";
    public string Version { get; set; } = "1.11.3";
    public long UptimeSeconds { get; set; }
    public double MemoryUsageMb { get; set; }
    public string Os { get; set; } = string.Empty;
    public int ConnectedMqttClients { get; set; }
    public int ActiveDrones { get; set; }
    public long Timestamp { get; set; }
}

public class DeviceStatusDto
{
    public string Sn { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;  // IP de origen de la conexión MQTT
    public string DeviceType { get; set; } = "Dron"; // Dron, Dock, Mando, etc.
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>Altura ASL — campo "height" DJI. Relativa al elipsoide terrestre (MSL/ASL).</summary>
    public double Altitude { get; set; }
    /// <summary>Altura AGL — campo "elevation" DJI. Relativa al punto de despegue del operador.</summary>
    public double Elevation { get; set; }
    public double Heading { get; set; }
    public double GimbalPitch { get; set; }
    public double GimbalRoll { get; set; }
    public double GimbalYaw { get; set; }
    public double ZoomFactor { get; set; }
    public int BatteryPercent { get; set; } = 100;
    public int GpsNumber { get; set; }
    public double SdrFreqBand { get; set; }
    public long LastSeen { get; set; }
    public bool IsOnline { get; set; }

    // mode_code según DJI Cloud API M3/M4 series (pilot-to-cloud MQTT)
    // 0=En tierra | 1-12,15-18=En vuelo | 13=Actualizando firmware | 14=Sin conexión RC
    public int ModeCode { get; set; } = -1;
    public bool IsFlying => ModeCode > 0 && ModeCode != 13 && ModeCode != 14;
    public int RemainFlightTime { get; set; } = -1;
    public double HorizontalSpeed { get; set; }
    public double VerticalSpeed { get; set; }
    public double AttitudePitch { get; set; }
    public double AttitudeRoll { get; set; }
    public bool GpsFixed { get; set; }
}

public class LogEventDto
{
    public long Timestamp { get; set; }
    public string Level { get; set; } = "INFO"; // INFO, WARN, ERROR, HTTP, MQTT
    public string Source { get; set; } = "System";
    public string Message { get; set; } = string.Empty;
}

public interface IAdminDataService
{
    void AddLog(string level, string source, string message);
    void RecordRequest(string method, string path, int statusCode, long elapsedMs, string ip);
    void RecordMqttEvent(string clientId, string topic, string action, string details);
    void UpdateDeviceTelemetry(
        string sn, double lat, double lon, double alt, double elevation, double heading, double pitch, double roll, double yaw, double zoom,
        int batteryPercent, int gpsNumber, double sdrFreqBand,
        int modeCode, int remainFlightTime, double horizontalSpeed, double verticalSpeed,
        double attitudePitch, double attitudeRoll, bool gpsFixed);
    void SetDeviceOnlineState(string sn, string clientId, string deviceType, bool isOnline);
    void SetDeviceTypeCode(string sn, int typeCode);
    int GetDeviceTypeCode(string sn);
    void SetDeviceSubtypeCode(string sn, int subtypeCode);
    int GetDeviceSubtypeCode(string sn);
    void RefreshDeviceStatus(string sn, int batteryPercent, int modeCode);
    void SetDeviceClientIp(string sn, string clientIp);
    void SetHmsCodes(string sn, List<HmsCodeDto> codes);
    List<HmsCodeDto>? GetHmsCodes(string sn);
    void SetLiveCapacity(string gatewaySn, LiveCapacityDto capacity);
    LiveCapacityDto? GetLiveCapacity(string gatewaySn);
    void SetLastStatePayload(string sn, string payload);
    string? GetLastStatePayload(string sn);
    void SetDrcActive(string gatewaySn, bool active);
    bool IsDrcActive(string gatewaySn);
    bool ShouldPushSignalR(string sn, int minIntervalMs = 100); // throttle para alta frecuencia
    void SetRcAircraftPairing(string rcSn, string aircraftSn);
    string? GetAircraftForGateway(string gatewaySn);
    void SetLastServicesReply(string gatewaySn, string method, int result);
    ServicesReplyDto? GetLastServicesReply(string gatewaySn);
    List<GatewayInfoDto> GetGateways();
    SystemStatusDto GetSystemStatus(int connectedMqttClientsCount);
    List<DeviceStatusDto> GetDevices();
    List<LogEventDto> GetLogs();
}

public class LiveVideoDto   { public string VideoIndex { get; set; } = ""; public int Status { get; set; } }
public class LiveCameraDto  { public string CameraIndex { get; set; } = ""; public List<LiveVideoDto> VideoList { get; set; } = new(); }
public class LiveDeviceDto  { public string Sn { get; set; } = ""; public List<LiveCameraDto> CameraList { get; set; } = new(); }
public class LiveCapacityDto{ public string GatewaySn { get; set; } = ""; public List<LiveDeviceDto> DeviceList { get; set; } = new(); }

public class PersistentStateDto
{
    public Dictionary<string, LiveCapacityDto> LiveCapacity { get; set; } = new();
    public Dictionary<string, string> RcToAircraft { get; set; } = new();
    public Dictionary<string, string> AircraftToRc { get; set; } = new();
    public Dictionary<string, string> LastStatePayloads { get; set; } = new();
    public Dictionary<string, int> DeviceTypeCodes { get; set; } = new();
    public Dictionary<string, int> DeviceSubtypeCodes { get; set; } = new();
}

public class HmsCodeDto
{
    public int  ComponentIndex { get; set; }
    public long Code           { get; set; }
    public int  Level          { get; set; }
    // Level: 0=Aviso  1=Advertencia  2=Error grave
}

public class GatewayInfoDto
{
    public string  GatewaySn      { get; set; } = string.Empty;
    public string  DeviceType     { get; set; } = string.Empty;
    public bool    IsOnline       { get; set; }
    public int     BatteryPercent { get; set; } = -1;
    public long    LastSeen       { get; set; }
    public string  ClientIp       { get; set; } = string.Empty;  // IP de origen MQTT del RC
    public string? AircraftSn     { get; set; }
    public bool    AircraftOnline { get; set; }
}

public class ServicesReplyDto
{
    public string GatewaySn  { get; set; } = string.Empty;
    public string Method     { get; set; } = string.Empty;
    public int    Result     { get; set; }
    public long   Timestamp  { get; set; }
}

public class AdminDataService : IAdminDataService
{
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly ConcurrentQueue<LogEventDto> _logs = new();
    private readonly ConcurrentDictionary<string, DeviceStatusDto> _devices = new();
    private readonly ConcurrentDictionary<string, int>    _deviceTypeCodes = new();
    private readonly ConcurrentDictionary<string, string> _rcToAircraft    = new();
    private readonly ConcurrentDictionary<string, string> _aircraftToRc    = new();
    private readonly ConcurrentDictionary<string, ServicesReplyDto> _servicesReplies = new();
    private readonly ConcurrentDictionary<string, List<HmsCodeDto>>  _hmsCodes     = new();
    private readonly ConcurrentDictionary<string, LiveCapacityDto>   _liveCapacity = new();
    private readonly ConcurrentDictionary<string, string>            _lastStatePayloads = new();
    private readonly ConcurrentDictionary<string, int>               _deviceSubtypeCodes = new();
    private const int MaxLogs = 100;

    private readonly string _cacheFilePath;
    private readonly ILogger<AdminDataService> _logger;
    private readonly IMqttFileLogger _fileLogger;

    public AdminDataService(ILogger<AdminDataService> logger, IMqttFileLogger fileLogger)
    {
        _logger = logger;
        _fileLogger = fileLogger;
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _cacheFilePath = Path.Combine(appDir, "live_capacity_cache.json");
        LoadCache();
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var state = JsonSerializer.Deserialize<PersistentStateDto>(json);
                if (state != null)
                {
                    if (state.LiveCapacity != null)
                    {
                        foreach (var kvp in state.LiveCapacity)
                            _liveCapacity[kvp.Key] = kvp.Value;
                    }
                    if (state.RcToAircraft != null)
                    {
                        foreach (var kvp in state.RcToAircraft)
                            _rcToAircraft[kvp.Key] = kvp.Value;
                    }
                    if (state.AircraftToRc != null)
                    {
                        foreach (var kvp in state.AircraftToRc)
                            _aircraftToRc[kvp.Key] = kvp.Value;
                    }
                    if (state.LastStatePayloads != null)
                    {
                        foreach (var kvp in state.LastStatePayloads)
                            _lastStatePayloads[kvp.Key] = kvp.Value;
                    }
                    if (state.DeviceTypeCodes != null)
                    {
                        foreach (var kvp in state.DeviceTypeCodes)
                            _deviceTypeCodes[kvp.Key] = kvp.Value;
                    }
                    if (state.DeviceSubtypeCodes != null)
                    {
                        foreach (var kvp in state.DeviceSubtypeCodes)
                            _deviceSubtypeCodes[kvp.Key] = kvp.Value;
                    }
                }
                AddLog("INFO", "AdminDataService", $"Cargados {_liveCapacity.Count} registros de live_capacity de caché persistente.");
            }
        }
        catch (Exception ex)
        {
            AddLog("ERROR", "AdminDataService", $"Error al cargar caché persistente: {ex.Message}");
        }
    }

    private void SaveCache()
    {
        try
        {
            var state = new PersistentStateDto
            {
                LiveCapacity = new Dictionary<string, LiveCapacityDto>(_liveCapacity),
                RcToAircraft = new Dictionary<string, string>(_rcToAircraft),
                AircraftToRc = new Dictionary<string, string>(_aircraftToRc),
                LastStatePayloads = new Dictionary<string, string>(_lastStatePayloads),
                DeviceTypeCodes = new Dictionary<string, int>(_deviceTypeCodes),
                DeviceSubtypeCodes = new Dictionary<string, int>(_deviceSubtypeCodes)
            };
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdminDataService] Error al guardar caché persistente en {Path}", _cacheFilePath);
        }
    }

    public void AddLog(string level, string source, string message)
    {
        _logs.Enqueue(new LogEventDto
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Level = level,
            Source = source,
            Message = message
        });

        // Mantener el tamaño del buffer limitado
        while (_logs.Count > MaxLogs)
        {
            _logs.TryDequeue(out _);
        }

        // Registrar en el log de archivo unificado, EXCEPTO si es de nivel MQTT (para evitar redundancia, 
        // ya que el tráfico MQTT se registra en detalle con su propio payload y topic en Program.cs)
        if (level != "MQTT")
        {
            _fileLogger.Log(source, level, payload: message);
        }
    }

    public void RecordRequest(string method, string path, int statusCode, long elapsedMs, string ip)
    {
        AddLog("HTTP", "WebAPI", $"{method} {path} - {statusCode} in {elapsedMs}ms (IP: {ip})");
    }

    public void RecordMqttEvent(string clientId, string topic, string action, string details)
    {
        AddLog("MQTT", "Broker", $"[{action}] ClientId: {clientId} | Topic: {topic} | {details}");
    }

    public void UpdateDeviceTelemetry(
        string sn, double lat, double lon, double alt, double elevation, double heading, double pitch, double roll, double yaw, double zoom,
        int batteryPercent, int gpsNumber, double sdrFreqBand,
        int modeCode, int remainFlightTime, double horizontalSpeed, double verticalSpeed,
        double attitudePitch, double attitudeRoll, bool gpsFixed)
    {
        var dev = _devices.GetOrAdd(sn, key => new DeviceStatusDto
        {
            Sn = key,
            DeviceType = "Dron/Dock",
            IsOnline = true
        });

        lock (dev)
        {
            dev.Latitude   = lat;
            dev.Longitude  = lon;
            dev.Altitude   = alt;        // ASL: campo "height" DJI (elipsoide)
            dev.Elevation  = elevation;  // AGL: campo "elevation" DJI (relativo al despegue)
            dev.Heading    = heading;
            dev.GimbalPitch = pitch;
            dev.GimbalRoll = roll;
            dev.GimbalYaw = yaw;
            dev.ZoomFactor = zoom;
            dev.BatteryPercent = batteryPercent;
            dev.GpsNumber = gpsNumber;
            dev.SdrFreqBand = sdrFreqBand;
            dev.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            dev.IsOnline = true;

            // Persistent fields
            if (modeCode >= 0) dev.ModeCode = modeCode;
            if (remainFlightTime >= 0) dev.RemainFlightTime = remainFlightTime;
            dev.HorizontalSpeed = horizontalSpeed;
            dev.VerticalSpeed = verticalSpeed;
            dev.AttitudePitch = attitudePitch;
            dev.AttitudeRoll = attitudeRoll;
            dev.GpsFixed = gpsFixed;
        }
    }

    public void SetDeviceOnlineState(string sn, string clientId, string deviceType, bool isOnline)
    {
        var dev = _devices.GetOrAdd(sn, key => new DeviceStatusDto { Sn = key });
        lock (dev)
        {
            dev.ClientId = clientId;
            if (!string.IsNullOrEmpty(deviceType)) dev.DeviceType = deviceType;
            dev.IsOnline = isOnline;
            dev.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        AddLog(isOnline ? "INFO" : "WARN", "Device", $"Dispositivo {sn} ({deviceType}) está {(isOnline ? "ONLINE" : "OFFLINE")}");
    }

    public SystemStatusDto GetSystemStatus(int connectedMqttClientsCount)
    {
        var process = Process.GetCurrentProcess();
        return new SystemStatusDto
        {
            Status = "OK",
            Version = "1.11.3",
            UptimeSeconds = (long)(DateTime.UtcNow - _startTime).TotalSeconds,
            MemoryUsageMb = process.PrivateMemorySize64 / (1024.0 * 1024.0),
            Os = RuntimeInformation.OSDescription,
            ConnectedMqttClients = connectedMqttClientsCount,
            ActiveDrones = _devices.Values.Count(d => d.IsOnline && d.DeviceType != "Cliente MQTT" && (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - d.LastSeen < 60000)),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    // Tiempo sin OSD para considerar un dron offline.
    // Debe coincidir con DRONE_OFFLINE_MS en map.html para que el frontend
    // y la REST API estén de acuerdo en qué drones están "online".
    // A 0.5 Hz (modo normal) equivale a ~7 paquetes OSD perdidos.
    private const long DroneOfflineMs = 15_000;

    public List<DeviceStatusDto> GetDevices()
    {
        var cutoff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - DroneOfflineMs;
        foreach (var dev in _devices.Values)
        {
            // Mando y Dock: su estado online lo gestiona exclusivamente el evento
            // MQTT connect/disconnect — no aplicar cutoff de tiempo.
            if (dev.DeviceType is "Mando" or "Dock") continue;
            if (dev.IsOnline && dev.DeviceType != "Cliente MQTT" && dev.LastSeen < cutoff)
            {
                lock (dev) { dev.IsOnline = false; }
                AddLog("WARN", "Device", $"Dispositivo {dev.Sn} marcado como OFFLINE por inactividad (>{DroneOfflineMs / 1000}s sin OSD)");
            }
        }
        return _devices.Values.OrderByDescending(d => d.LastSeen).ToList();
    }

    public List<LogEventDto> GetLogs()
    {
        return _logs.ToList();
    }

    public void RefreshDeviceStatus(string sn, int batteryPercent, int modeCode)
    {
        if (!_devices.TryGetValue(sn, out var dev)) return;
        lock (dev)
        {
            dev.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (batteryPercent >= 0) dev.BatteryPercent = batteryPercent;
            if (modeCode >= 0) dev.ModeCode = modeCode;
        }
    }

    public void SetDeviceClientIp(string sn, string clientIp)
    {
        var dev = _devices.GetOrAdd(sn, key => new DeviceStatusDto { Sn = key });
        lock (dev) { dev.ClientIp = clientIp; }
    }

    public void SetHmsCodes(string sn, List<HmsCodeDto> codes)
        => _hmsCodes[sn] = codes;

    public List<HmsCodeDto>? GetHmsCodes(string sn)
        => _hmsCodes.TryGetValue(sn, out var c) ? c : null;

    public void SetLiveCapacity(string sn, LiveCapacityDto capacity)
    {
        _liveCapacity[sn] = capacity;
        if (_rcToAircraft.TryGetValue(sn, out var aircraftSn) && !string.IsNullOrEmpty(aircraftSn))
        {
            _liveCapacity[aircraftSn] = capacity;
        }
        else if (_aircraftToRc.TryGetValue(sn, out var gatewaySn) && !string.IsNullOrEmpty(gatewaySn))
        {
            _liveCapacity[gatewaySn] = capacity;
        }
        SaveCache();
    }

    private LiveCapacityDto? GetDefaultLiveCapacity(string gatewaySn, string aircraftSn, int typeCode, int subtypeCode)
    {
        // Si no conocemos el typeCode, por defecto asumimos Mavic 3T (77/1)
        if (typeCode <= 0)
        {
            typeCode = 77;
            subtypeCode = 1;
        }

        var devDto = new LiveDeviceDto { Sn = aircraftSn };

        if (typeCode == 77) // Mavic 3 Series
        {
            if (subtypeCode == 1) // Mavic 3T
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "67-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "infra-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
            else // Mavic 3E
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "66-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
        }
        else if (typeCode == 67) // M30 Series
        {
            if (subtypeCode == 1) // M30T
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "53-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "infra-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
            else // M30
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "52-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
        }
        else if (typeCode == 99) // M4 Series (M4T/M4TD)
        {
            if (subtypeCode == 1) // M4T (Thermal)
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "89-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "infra-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
            else // M4TD or other
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "99-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "infra-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
        }
        else if (typeCode == 92) // M3D/M3TD Series
        {
            if (subtypeCode == 1) // M3TD (Thermal)
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "80-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 },
                        new LiveVideoDto { VideoIndex = "infra-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
            else // M3D
            {
                var camDto = new LiveCameraDto
                {
                    CameraIndex = "80-0-0",
                    VideoList = new List<LiveVideoDto>
                    {
                        new LiveVideoDto { VideoIndex = "normal-0", Status = 0 }
                    }
                };
                devDto.CameraList.Add(camDto);
            }
        }
        else
        {
            // Fallback genérico para Mavic 3T u otros
            var camDto = new LiveCameraDto
            {
                CameraIndex = "67-0-0",
                VideoList = new List<LiveVideoDto>
                {
                    new LiveVideoDto { VideoIndex = "normal-0", Status = 0 },
                    new LiveVideoDto { VideoIndex = "zoom-0", Status = 0 },
                    new LiveVideoDto { VideoIndex = "infra-0", Status = 0 }
                }
            };
            devDto.CameraList.Add(camDto);
        }

        return new LiveCapacityDto
        {
            GatewaySn = gatewaySn,
            DeviceList = new List<LiveDeviceDto> { devDto }
        };
    }

    public LiveCapacityDto? GetLiveCapacity(string sn)
    {
        if (string.IsNullOrEmpty(sn)) return null;

        // Intentar obtener de la caché en memoria primero
        if (_liveCapacity.TryGetValue(sn, out var c) && c.DeviceList?.Any(d => d.CameraList != null && d.CameraList.Count > 0) == true)
        {
            return c;
        }

        string? aircraftSn = null;
        string? gatewaySn = null;

        if (_rcToAircraft.TryGetValue(sn, out var acSn) && !string.IsNullOrEmpty(acSn))
        {
            aircraftSn = acSn;
            gatewaySn = sn;
        }
        else if (_aircraftToRc.TryGetValue(sn, out var gwSn) && !string.IsNullOrEmpty(gwSn))
        {
            gatewaySn = gwSn;
            aircraftSn = sn;
        }
        else
        {
            // Si no hay emparejamiento explícito, deducir según el código de tipo
            var typeCodeVal = _deviceTypeCodes.GetValueOrDefault(sn, -1);
            if (typeCodeVal == 144) // Mando
            {
                gatewaySn = sn;
            }
            else if (typeCodeVal > 0) // Aeronave
            {
                aircraftSn = sn;
            }
        }

        // Si tenemos un gateway y/o una aeronave, intentar sintetizar capacidad
        if (!string.IsNullOrEmpty(gatewaySn) || !string.IsNullOrEmpty(aircraftSn))
        {
            var effGatewaySn = gatewaySn ?? sn;
            var effAircraftSn = aircraftSn ?? sn;

            var typeCode = _deviceTypeCodes.GetValueOrDefault(effAircraftSn, -1);
            var subtypeCode = _deviceSubtypeCodes.GetValueOrDefault(effAircraftSn, -1);

            if (typeCode != 144) // No sintetizar para un mando sin aeronave real (o asumir como dron)
            {
                var fallbackCap = GetDefaultLiveCapacity(effGatewaySn, effAircraftSn, typeCode, subtypeCode);
                if (fallbackCap != null)
                {
                    _liveCapacity[effGatewaySn] = fallbackCap;
                    _liveCapacity[effAircraftSn] = fallbackCap;
                    SaveCache();
                    return fallbackCap;
                }
            }
        }

        // Último recurso: intentar ver si hay un objeto en caché a pesar de no tener cámaras
        if (_liveCapacity.TryGetValue(sn, out var fallbackCache)) return fallbackCache;

        return null;
    }


    public void SetLastStatePayload(string sn, string payload)
    {
        _lastStatePayloads[sn] = payload;
        SaveCache();
    }

    public string? GetLastStatePayload(string sn)
    {
        return _lastStatePayloads.TryGetValue(sn, out var p) ? p : null;
    }

    private readonly ConcurrentDictionary<string, bool> _drcActive         = new();
    private readonly ConcurrentDictionary<string, long> _lastSignalRPush   = new();

    public void SetDrcActive(string gatewaySn, bool active)
        => _drcActive[gatewaySn] = active;

    public bool IsDrcActive(string gatewaySn)
        => _drcActive.TryGetValue(gatewaySn, out var v) && v;

    public bool ShouldPushSignalR(string sn, int minIntervalMs = 100)
    {
        var now  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var last = _lastSignalRPush.GetOrAdd(sn, 0L);
        if (now - last < minIntervalMs) return false;
        _lastSignalRPush[sn] = now;
        return true;
    }

    public void SetDeviceTypeCode(string sn, int typeCode)
    {
        _deviceTypeCodes[sn] = typeCode;
        // Actualizar también el string legible en _devices para que GetDevices() lo exponga correctamente
        var typeName = typeCode == 144 ? "Mando" : "Dron";
        if (_devices.TryGetValue(sn, out var dev))
            lock (dev) { dev.DeviceType = typeName; }
        SaveCache();
    }

    public int GetDeviceTypeCode(string sn)
        => _deviceTypeCodes.GetValueOrDefault(sn, -1);

    public void SetDeviceSubtypeCode(string sn, int subtypeCode)
    {
        _deviceSubtypeCodes[sn] = subtypeCode;
        SaveCache();
    }

    public int GetDeviceSubtypeCode(string sn)
        => _deviceSubtypeCodes.GetValueOrDefault(sn, -1);

    public void SetRcAircraftPairing(string rcSn, string aircraftSn)
    {
        _rcToAircraft[rcSn]       = aircraftSn;
        _aircraftToRc[aircraftSn] = rcSn;
        
        if (_liveCapacity.TryGetValue(rcSn, out var cap))
        {
            _liveCapacity[aircraftSn] = cap;
        }
        else if (_liveCapacity.TryGetValue(aircraftSn, out var cap2))
        {
            _liveCapacity[rcSn] = cap2;
        }
        SaveCache();
    }

    public string? GetAircraftForGateway(string gatewaySn)
        => _rcToAircraft.TryGetValue(gatewaySn, out var sn) ? sn : null;

    public void SetLastServicesReply(string gatewaySn, string method, int result)
    {
        _servicesReplies[gatewaySn] = new ServicesReplyDto
        {
            GatewaySn = gatewaySn,
            Method    = method,
            Result    = result,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        var ok = result == 0 ? "OK" : $"ERROR({result})";
        AddLog(result == 0 ? "INFO" : "WARN", "LiveStream",
            $"services_reply [{method}] → {gatewaySn}: {ok}");
    }

    public ServicesReplyDto? GetLastServicesReply(string gatewaySn)
        => _servicesReplies.TryGetValue(gatewaySn, out var r) ? r : null;

    public List<GatewayInfoDto> GetGateways()
    {
        var result = new List<GatewayInfoDto>();

        foreach (var dev in _devices.Values)
        {
            if (dev.DeviceType != "Mando" && dev.DeviceType != "Dock") continue;
            // Los gateways (RC/Dock) usan el flag IsOnline directamente —
            // no aplican el cutoff de 45s porque no mandan telemetría periódica.
            // El flag se gestiona por los eventos MQTT connect/disconnect.
            bool online = dev.IsOnline;

            var info = new GatewayInfoDto
            {
                GatewaySn      = dev.Sn,
                DeviceType     = dev.DeviceType,
                IsOnline       = online,
                BatteryPercent = dev.BatteryPercent,
                LastSeen       = dev.LastSeen,
                ClientIp       = dev.ClientIp,
            };

            // Buscar aeronave pareada (sí aplica cutoff de 45s para el dron, no para el gateway)
            if (_rcToAircraft.TryGetValue(dev.Sn, out var acSn))
            {
                var acCutoff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 45000;
                info.AircraftSn = acSn;
                if (_devices.TryGetValue(acSn, out var acDev))
                    info.AircraftOnline = acDev.IsOnline && acDev.LastSeen >= acCutoff;
            }

            result.Add(info);
        }

        return result.OrderByDescending(g => g.IsOnline).ThenBy(g => g.GatewaySn).ToList();
    }
}
