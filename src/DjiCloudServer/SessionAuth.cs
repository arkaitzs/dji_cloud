namespace DjiCloudServer;

/// <summary>
/// Token de sesión único por vida del servidor.
/// Lo entregan /api/dji/config y /manage/api/v1/login, y lo valida /token/refresh.
/// (Sustituible por JWT real cuando se implemente la autenticación completa.)
/// </summary>
public static class SessionAuth
{
    public static readonly string Token = $"session_{Guid.NewGuid():N}";

    public static bool IsValid(string? token) =>
        !string.IsNullOrEmpty(token) && string.Equals(token, Token, StringComparison.Ordinal);
}
