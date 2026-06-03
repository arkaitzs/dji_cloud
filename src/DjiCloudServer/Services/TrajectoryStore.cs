using System.Collections.Concurrent;

namespace DjiCloudServer.Services;

public class DronePositionDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public long Timestamp { get; set; } // Unix timestamp in milliseconds
}

public interface ITrajectoryStore
{
    void AddPosition(string sn, double lat, double lon, double alt);
    List<DronePositionDto> GetTrajectory(string sn);
    Dictionary<string, List<DronePositionDto>> GetAllTrajectories();
    void ClearTrajectory(string sn);
}

public class TrajectoryStore : ITrajectoryStore
{
    private readonly ConcurrentDictionary<string, List<DronePositionDto>> _trajectories = new();
    private const int MaxPointsPerDrone = 2000;

    // Distancia mínima (en grados ≈ ~2m) entre puntos consecutivos para reducir densidad
    private const double MinDeltaDeg = 0.00002;

    public void AddPosition(string sn, double lat, double lon, double alt)
    {
        var list = _trajectories.GetOrAdd(sn, _ => new List<DronePositionDto>());
        lock (list)
        {
            // Filtro de distancia mínima: descartar puntos demasiado cercanos al anterior
            if (list.Count > 0)
            {
                var last = list[^1];
                var dLat = Math.Abs(lat - last.Latitude);
                var dLon = Math.Abs(lon - last.Longitude);
                if (dLat < MinDeltaDeg && dLon < MinDeltaDeg)
                    return; // Dron prácticamente quieto — no duplicar punto
            }

            list.Add(new DronePositionDto
            {
                Latitude = lat,
                Longitude = lon,
                Altitude = alt,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            if (list.Count > MaxPointsPerDrone)
                list.RemoveAt(0);
        }
    }

    public List<DronePositionDto> GetTrajectory(string sn)
    {
        if (_trajectories.TryGetValue(sn, out var list))
        {
            lock (list)
            {
                return list.ToList();
            }
        }
        return new List<DronePositionDto>();
    }

    public Dictionary<string, List<DronePositionDto>> GetAllTrajectories()
    {
        var result = new Dictionary<string, List<DronePositionDto>>();
        foreach (var kvp in _trajectories)
        {
            lock (kvp.Value)
            {
                result[kvp.Key] = kvp.Value.ToList();
            }
        }
        return result;
    }

    public void ClearTrajectory(string sn)
    {
        if (_trajectories.TryGetValue(sn, out var list))
        {
            lock (list)
            {
                list.Clear();
            }
        }
    }
}
