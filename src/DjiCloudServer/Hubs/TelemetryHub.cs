using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace DjiCloudServer.Hubs;

/// <summary>
/// Hub de SignalR para transmitir la telemetría del dron en tiempo real al mapa web.
///
/// Scoping multi-workspace: cada cliente se une al grupo "workspace:{id}" al conectar
/// (workspace de la query ?workspace_id= o el por defecto de configuración).
/// Nota: los emisores actuales usan Clients.All — equivalente con un único workspace.
/// Para multi-workspace real, migrar los emisores a Clients.Group(WorkspaceGroup(id)).
/// </summary>
public class TelemetryHub : Hub
{
    private readonly ILogger<TelemetryHub> _logger;
    private readonly string _defaultWorkspaceId;

    public TelemetryHub(ILogger<TelemetryHub> logger, IOptions<DjiCloudOptions> options)
    {
        _logger = logger;
        _defaultWorkspaceId = options.Value.WorkspaceId;
    }

    public static string WorkspaceGroup(string workspaceId) => $"workspace:{workspaceId}";

    public override async Task OnConnectedAsync()
    {
        var workspaceId = Context.GetHttpContext()?.Request.Query["workspace_id"].FirstOrDefault()
            ?? _defaultWorkspaceId;
        await Groups.AddToGroupAsync(Context.ConnectionId, WorkspaceGroup(workspaceId));

        _logger.LogInformation("Cliente conectado al TelemetryHub: {ConnectionId} (workspace {WorkspaceId})",
            Context.ConnectionId, workspaceId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente desconectado del TelemetryHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
