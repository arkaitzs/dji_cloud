namespace DjiCloudServer.Services;

/// <summary>
/// Escritura/lectura atómica de ficheros JSON de persistencia (#2.7).
///
/// Problema que resuelve: File.WriteAllText trunca el fichero antes de escribir;
/// un crash o corte de luz a mitad de escritura deja el store corrupto y se
/// pierden todos los datos (map_elements.json, live_capacity_cache.json...).
///
/// Patrón: escribir a {path}.tmp y promoverlo con File.Replace (rename atómico
/// en NTFS), conservando la versión anterior como {path}.bak. La lectura
/// recupera del .bak si el fichero principal está corrupto o ausente.
///
/// Nota: para producción multi-proceso o alto volumen, el siguiente escalón es
/// SQLite con WAL; este patrón elimina la corrupción por crash con cambios mínimos.
/// </summary>
public static class AtomicJsonFile
{
    /// <summary>Escribe contenido de forma atómica, preservando backup .bak.</summary>
    public static void Write(string path, string content)
    {
        var tmpPath = path + ".tmp";
        var bakPath = path + ".bak";

        File.WriteAllText(tmpPath, content);

        if (File.Exists(path))
        {
            // Reemplazo atómico: path → .bak, .tmp → path (una sola operación NTFS)
            File.Replace(tmpPath, path, bakPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tmpPath, path);
        }
    }

    /// <summary>
    /// Lee el fichero validándolo con <paramref name="tryParse"/>.
    /// Si el principal está corrupto/ausente, intenta el backup .bak.
    /// Devuelve default si ninguno es recuperable.
    /// </summary>
    public static T? ReadWithRecovery<T>(string path, Func<string, T?> tryParse, Action<string>? onRecovered = null)
    {
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            try
            {
                if (!File.Exists(candidate)) continue;
                var content = File.ReadAllText(candidate);
                var parsed = tryParse(content);
                if (parsed != null)
                {
                    if (candidate != path)
                        onRecovered?.Invoke(candidate);
                    return parsed;
                }
            }
            catch
            {
                // Probar el siguiente candidato (.bak)
            }
        }
        return default;
    }
}
