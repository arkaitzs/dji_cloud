namespace DjiCloudServer.Services;

/// <summary>
/// Parser de paquetes MISB ST 0601 (KLV binario).
/// Soporta ficheros .klv standalone con uno o múltiples paquetes Universal Set.
/// </summary>
public static class KlvParser
{
    // Universal Label MISB ST 0601 (16 bytes)
    private static readonly byte[] ST0601_UL =
    [
        0x06, 0x0E, 0x2B, 0x34, 0x02, 0x0B, 0x01, 0x01,
        0x0E, 0x01, 0x03, 0x01, 0x01, 0x00, 0x00, 0x00
    ];

    /// <summary>Parsea un fichero KLV binario y devuelve todos los frames como diccionarios.</summary>
    public static List<Dictionary<string, object?>> ParseFile(byte[] data, int maxFrames = 5000)
    {
        var frames = new List<Dictionary<string, object?>>();
        int pos = 0;

        while (pos < data.Length - 16 && frames.Count < maxFrames)
        {
            int ulPos = FindUl(data, pos);
            if (ulPos < 0) break;

            pos = ulPos + 16;
            int length = ReadBerLength(data, ref pos);
            if (length < 0 || pos + length > data.Length) break;

            var frame = ParseLds(data, pos, length);
            if (frame.Count > 0) frames.Add(frame);

            pos += length;
        }

        return frames;
    }

    // ─── Helpers de búsqueda y lectura ───────────────────────────────────────

    private static int FindUl(byte[] data, int start)
    {
        for (int i = start; i <= data.Length - 16; i++)
        {
            bool match = true;
            for (int j = 0; j < 16; j++)
                if (data[i + j] != ST0601_UL[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    private static int ReadBerLength(byte[] data, ref int pos)
    {
        if (pos >= data.Length) return -1;
        byte first = data[pos++];
        if (first < 0x80) return first;

        int numBytes = first & 0x7F;
        if (numBytes > 4 || pos + numBytes > data.Length) return -1;

        int length = 0;
        for (int i = 0; i < numBytes; i++)
            length = (length << 8) | data[pos++];
        return length;
    }

    // ─── Parser del Local Data Set (LDS) ─────────────────────────────────────

    private static Dictionary<string, object?> ParseLds(byte[] data, int start, int length)
    {
        var result = new Dictionary<string, object?>();
        int pos = start;
        int end = start + length;

        while (pos < end - 1)
        {
            int tag = data[pos++];
            if (tag == 1) { pos += 2; continue; } // Skip checksum (tag 1, 2 bytes)

            int len = ReadBerLength(data, ref pos);
            if (len < 0 || pos + len > end) break;

            var value = data.AsSpan(pos, len).ToArray();
            pos += len;

            DecodeTag(result, tag, value);
        }

        return result;
    }

    // ─── Decodificación de tags MISB ST 0601 ─────────────────────────────────

    private static void DecodeTag(Dictionary<string, object?> d, int tag, byte[] v)
    {
        switch (tag)
        {
            // Tag 2 — Precision Time Stamp (uint64, microsegundos desde Unix epoch)
            case 2 when v.Length == 8:
                long us = 0;
                for (int i = 0; i < 8; i++) us = (us << 8) | v[i];
                d["tag2_timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(us / 1000)
                                                    .ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                d["tag2_timestamp_us"] = us;
                break;

            // Tag 5 — Platform Heading Angle [0, 360) uint16
            case 5 when v.Length == 2:
                d["tag5_platformHeading"] = Round(MapU(ReadU(v, 2), 0, 65535, 0, 360));
                break;

            // Tag 6 — Platform Pitch Angle [-20, 20] int16
            case 6 when v.Length == 2:
                d["tag6_platformPitch"] = Round(MapI(ReadI(v, 2), -32768, 32767, -20, 20));
                break;

            // Tag 7 — Platform Roll Angle [-50, 50] int16
            case 7 when v.Length == 2:
                d["tag7_platformRoll"] = Round(MapI(ReadI(v, 2), -32768, 32767, -50, 50));
                break;

            // Tag 10 — Platform Designation (string)
            case 10:
                d["tag10_designation"] = System.Text.Encoding.ASCII.GetString(v).TrimEnd('\0');
                break;

            // Tag 11 — Image Source Sensor (string)
            case 11:
                d["tag11_imageSensor"] = System.Text.Encoding.ASCII.GetString(v).TrimEnd('\0');
                break;

            // Tag 13 — Sensor Latitude [-90, 90] int32
            case 13 when v.Length == 4:
                d["tag13_sensorLat"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -90, 90), 7);
                break;

            // Tag 14 — Sensor Longitude [-180, 180] int32
            case 14 when v.Length == 4:
                d["tag14_sensorLon"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180), 7);
                break;

            // Tag 15 — Sensor True Altitude [-900, 19000] uint16
            case 15 when v.Length == 2:
                d["tag15_sensorAlt"] = Round(MapU(ReadU(v, 2), 0, 65535, -900, 19000));
                break;

            // Tag 16 — Sensor HFOV [0, 180] uint16
            case 16 when v.Length == 2:
                d["tag16_hFov"] = Round(MapU(ReadU(v, 2), 0, 65535, 0, 180));
                break;

            // Tag 17 — Sensor VFOV [0, 180] uint16
            case 17 when v.Length == 2:
                d["tag17_vFov"] = Round(MapU(ReadU(v, 2), 0, 65535, 0, 180));
                break;

            // Tag 18 — Sensor Relative Azimuth [0, 360) uint32
            case 18 when v.Length == 4:
                d["tag18_sensorAzimuth"] = Round(MapU(ReadU(v, 4), 0, 4294967295L, 0, 360));
                break;

            // Tag 19 — Sensor Relative Elevation [-180, 180] int32
            case 19 when v.Length == 4:
                d["tag19_sensorElevation"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180));
                break;

            // Tag 20 — Sensor Relative Roll [0, 360) uint32
            case 20 when v.Length == 4:
                d["tag20_sensorRoll"] = Round(MapU(ReadU(v, 4), 0, 4294967295L, 0, 360));
                break;

            // Tag 21 — Slant Range [0, 5000000] uint32
            case 21 when v.Length == 4:
                d["tag21_slantRange"] = Round(MapU(ReadU(v, 4), 0, 4294967295L, 0, 5_000_000));
                break;

            // Tag 22 — Target Width [0, 10000] uint16
            case 22 when v.Length == 2:
                d["tag22_targetWidth"] = Round(MapU(ReadU(v, 2), 0, 65535, 0, 10000));
                break;

            // Tag 23 — Frame Center Latitude [-90, 90] int32
            case 23 when v.Length == 4:
                d["tag23_frameCenterLat"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -90, 90), 7);
                break;

            // Tag 24 — Frame Center Longitude [-180, 180] int32
            case 24 when v.Length == 4:
                d["tag24_frameCenterLon"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180), 7);
                break;

            // Tag 25 — Frame Center Elevation [-900, 19000] uint16
            case 25 when v.Length == 2:
                d["tag25_frameCenterAlt"] = Round(MapU(ReadU(v, 2), 0, 65535, -900, 19000));
                break;

            // Tags 82-89 — Corner Latitude/Longitude (full precision, int32)
            case 82 when v.Length == 4: d["tag82_corner1Lat"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -90, 90), 7); break;
            case 83 when v.Length == 4: d["tag83_corner1Lon"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180), 7); break;
            case 84 when v.Length == 4: d["tag84_corner2Lat"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -90, 90), 7); break;
            case 85 when v.Length == 4: d["tag85_corner2Lon"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180), 7); break;
            case 86 when v.Length == 4: d["tag86_corner3Lat"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -90, 90), 7); break;
            case 87 when v.Length == 4: d["tag87_corner3Lon"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180), 7); break;
            case 88 when v.Length == 4: d["tag88_corner4Lat"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -90, 90), 7); break;
            case 89 when v.Length == 4: d["tag89_corner4Lon"] = Round(MapI(ReadI(v, 4), int.MinValue, int.MaxValue, -180, 180), 7); break;
        }
    }

    // ─── Lectura de enteros big-endian ────────────────────────────────────────

    private static long ReadU(byte[] v, int bytes)
    {
        long r = 0;
        for (int i = 0; i < bytes; i++) r = (r << 8) | v[i];
        return r;
    }

    private static long ReadI(byte[] v, int bytes)
    {
        long r = ReadU(v, bytes);
        long sign = 1L << (bytes * 8 - 1);
        if ((r & sign) != 0) r -= sign << 1;
        return r;
    }

    // ─── Mapeo lineal ─────────────────────────────────────────────────────────

    private static double MapU(long raw, long minR, long maxR, double minP, double maxP) =>
        minP + (double)(raw - minR) / (maxR - minR) * (maxP - minP);

    private static double MapI(long raw, long minR, long maxR, double minP, double maxP) =>
        minP + (double)(raw - minR) / (maxR - minR) * (maxP - minP);

    private static double Round(double v, int decimals = 4) =>
        Math.Round(v, decimals, MidpointRounding.AwayFromZero);
}
