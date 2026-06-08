using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DjiCloudServer.Services;

public interface IFlightAreaService
{
    /// <summary>Devuelve la URL relativa del KML activo, o null si no hay ninguno.</summary>
    string? GetActiveKmlRelativePath();

    /// <summary>Lista todos los KMLs disponibles en el directorio.</summary>
    IEnumerable<string> GetAvailableKmls();

    /// <summary>Establece el KML activo por nombre de archivo.</summary>
    Task SetActiveKmlAsync(string filename);

    /// <summary>Guarda un KML subido y lo devuelve como nombre de archivo.</summary>
    Task<string> SaveKmlAsync(string filename, Stream content);

    /// <summary>Elimina un KML del directorio.</summary>
    bool DeleteKml(string filename);
}

public class FlightAreaService : IFlightAreaService
{
    private readonly string _kmlDir;
    private readonly string _activeFile;

    public FlightAreaService(IWebHostEnvironment env)
    {
        _kmlDir    = Path.Combine(env.WebRootPath, "flight-areas");
        _activeFile = Path.Combine(_kmlDir, ".active");
        Directory.CreateDirectory(_kmlDir);
    }

    public string? GetActiveKmlRelativePath()
    {
        if (!File.Exists(_activeFile)) return null;
        var name = File.ReadAllText(_activeFile).Trim();
        if (string.IsNullOrEmpty(name)) return null;
        var full = Path.Combine(_kmlDir, name);
        return File.Exists(full) ? $"flight-areas/{name}" : null;
    }

    public IEnumerable<string> GetAvailableKmls()
    {
        if (!Directory.Exists(_kmlDir)) return [];
        return Directory.GetFiles(_kmlDir, "*.kml")
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Select(f => f!)
                        .OrderBy(f => f);
    }

    public async Task SetActiveKmlAsync(string filename)
    {
        // Valida que existe en el directorio
        var safeName = Path.GetFileName(filename);
        var full     = Path.Combine(_kmlDir, safeName);
        if (!File.Exists(full))
            throw new FileNotFoundException($"KML '{safeName}' no encontrado.");
        await File.WriteAllTextAsync(_activeFile, safeName);
    }

    public async Task<string> SaveKmlAsync(string filename, Stream content)
    {
        var safeName = Path.GetFileName(filename);
        if (!safeName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase))
            safeName += ".kml";

        var dest = Path.Combine(_kmlDir, safeName);
        await using var fs = File.Create(dest);
        await content.CopyToAsync(fs);
        return safeName;
    }

    public bool DeleteKml(string filename)
    {
        var safeName = Path.GetFileName(filename);
        var full     = Path.Combine(_kmlDir, safeName);
        if (!File.Exists(full)) return false;

        File.Delete(full);

        // Si era el activo, limpiar la selección
        if (File.Exists(_activeFile) &&
            File.ReadAllText(_activeFile).Trim() == safeName)
            File.WriteAllText(_activeFile, "");

        return true;
    }
}
