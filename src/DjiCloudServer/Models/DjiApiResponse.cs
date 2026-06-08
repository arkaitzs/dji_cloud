using Newtonsoft.Json;

namespace DjiCloudServer.Models;

/// <summary>
/// Envelope de respuesta estándar de la DJI Cloud API.
/// Estructura: { "code": 0, "message": "success", "data": { ... } }
///
/// IMPORTANTE: DJI Pilot 2 comprueba la clave "code" en minúsculas.
/// Sin [JsonProperty] Newtonsoft serializa en PascalCase ("Code") y el RC
/// no reconoce el éxito, causando que el mando no confirme la operación.
/// </summary>
public class DjiApiResponse<T>
{
    /// <summary>Código de resultado: 0 = éxito, otro = error.</summary>
    [JsonProperty("code")]
    public int Code { get; set; }

    /// <summary>Mensaje descriptivo del resultado.</summary>
    [JsonProperty("message")]
    public string Message { get; set; } = "success";

    /// <summary>Datos de la respuesta.</summary>
    [JsonProperty("data")]
    public T? Data { get; set; }

    public static DjiApiResponse<T> Success(T data) => new()
    {
        Code    = 0,
        Message = "success",
        Data    = data
    };

    public static DjiApiResponse<T> Fail(int code, string message) => new()
    {
        Code    = code,
        Message = message,
        Data    = default
    };
}

/// <summary>Respuesta sin datos (operación exitosa sin payload).</summary>
public class DjiApiResponse : DjiApiResponse<object?>
{
    public static DjiApiResponse Ok() => new()
    {
        Code    = 0,
        Message = "success",
        Data    = null
    };
}
