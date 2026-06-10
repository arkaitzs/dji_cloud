using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Tactical Situation Awareness (TSA) — DJI Cloud API v1.11.x
/// Spec: doc 16 (Situation Awareness) + doc 56 (Obtain Device Topology List)
///
/// DJI Pilot 2 llama a GET /manage/api/v1/workspaces/{wid}/devices/topologies
/// en dos momentos:
///   1. Al conectar por primera vez (carga inicial de dispositivos en el mapa).
///   2. Al recibir un push WS device_online / device_offline / device_update_topo.
///
/// La respuesta define qué iconos aparecen en el mapa del RC para cada dron y mando.
/// Sin este endpoint, el RC no muestra otros drones aunque reciba device_osd pushes.
/// </summary>
[ApiController]
public class SituationAwarenessController : ControllerBase
{
    private readonly IAdminDataService _admin;
    private readonly ILogger<SituationAwarenessController> _logger;

    public SituationAwarenessController(
        IAdminDataService admin,
        ILogger<SituationAwarenessController> logger)
    {
        _admin  = admin;
        _logger = logger;
    }

    // GET /manage/api/v1/workspaces/{workspaceId}/devices/topologies
    [HttpGet("manage/api/v1/workspaces/{workspaceId}/devices/topologies")]
    public IActionResult GetTopologies(string workspaceId)
    {
        var gateways = _admin.GetGateways();
        var devices  = _admin.GetDevices();

        // Construir la lista de topologías: { hosts: [aeronave], parents: [RC/gateway] }
        // Si el gateway no tiene aeronave pareada, hosts estará vacío.
        // Si hay aeronaves sin gateway conocido (standalone), se añaden solas.
        var topoList  = new List<object>();
        var seenSnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var gw in gateways)
        {
            // Determinar device_model del gateway (RC/dock)
            var gwTypeCode    = _admin.GetDeviceTypeCode(gw.GatewaySn);
            var gwSubType     = _admin.GetDeviceSubtypeCode(gw.GatewaySn);
            var gwModelKey    = BuildModelKey(domain: 2, type: gwTypeCode > 0 ? gwTypeCode : 144, subType: gwSubType);

            var parentEntry = new
            {
                sn              = gw.GatewaySn,
                online_status   = gw.IsOnline,
                device_callsign = gw.GatewaySn,
                user_id         = "pilot",
                user_callsign   = gw.GatewaySn,
                device_model    = gwModelKey
                // icon_urls omitido → Pilot 2 usa el icono por defecto para RC
            };

            seenSnSet.Add(gw.GatewaySn);

            var hosts = new List<object>();
            if (!string.IsNullOrEmpty(gw.AircraftSn))
            {
                var acTypeCode = _admin.GetDeviceTypeCode(gw.AircraftSn);
                var acSubType  = _admin.GetDeviceSubtypeCode(gw.AircraftSn);
                var acModelKey = BuildModelKey(domain: 0, type: acTypeCode > 0 ? acTypeCode : 0, subType: acSubType);

                hosts.Add(new
                {
                    sn              = gw.AircraftSn,
                    online_status   = gw.AircraftOnline,
                    device_callsign = gw.AircraftSn,
                    user_id         = "pilot",
                    user_callsign   = gw.GatewaySn,
                    device_model    = acModelKey,
                    icon_urls       = new
                    {
                        normal_icon_url   = "resource://Pilot2/drawable/tsa_aircraft_others_normal",
                        selected_icon_url = "resource://Pilot2/drawable/tsa_aircraft_others_pressed"
                    }
                });
                seenSnSet.Add(gw.AircraftSn);
            }

            topoList.Add(new { hosts, parents = new[] { parentEntry } });
        }

        // Aeronaves sin gateway conocido (p.ej. conectadas directo por Dock o dispositivos no pareados)
        var unpaired = devices
            .Where(d => !seenSnSet.Contains(d.Sn)
                     && d.DeviceType is not ("Cliente MQTT" or "Mando")
                     && d.IsOnline)
            .ToList();

        foreach (var ac in unpaired)
        {
            var acTypeCode = _admin.GetDeviceTypeCode(ac.Sn);
            var acSubType  = _admin.GetDeviceSubtypeCode(ac.Sn);
            var acModelKey = BuildModelKey(domain: 0, type: acTypeCode > 0 ? acTypeCode : 0, subType: acSubType);

            topoList.Add(new
            {
                hosts = new[]
                {
                    new
                    {
                        sn              = ac.Sn,
                        online_status   = ac.IsOnline,
                        device_callsign = ac.Sn,
                        user_id         = "pilot",
                        user_callsign   = ac.Sn,
                        device_model    = acModelKey,
                        icon_urls       = new
                        {
                            normal_icon_url   = "resource://Pilot2/drawable/tsa_aircraft_others_normal",
                            selected_icon_url = "resource://Pilot2/drawable/tsa_aircraft_others_pressed"
                        }
                    }
                },
                parents = Array.Empty<object>()
            });
        }

        _logger.LogInformation(
            "[TSA] GET topologies workspace={WorkspaceId} → {Count} entradas ({Gateways} gateways, {Unpaired} sin gateway)",
            workspaceId, topoList.Count, gateways.Count, unpaired.Count);

        return Ok(new
        {
            code    = 0,
            message = "success",
            data    = new { list = topoList }
        });
    }

    /// <summary>
    /// Construye el objeto device_model según la spec DJI.
    /// key = "{domain}-{type}-{subType}"
    /// domain: 0=aeronave, 2=RC/gateway
    /// type/subType: códigos de tipo DJI (los mismos que vienen en update_topo MQTT)
    /// </summary>
    private static object BuildModelKey(int domain, int type, int subType)
    {
        return new
        {
            domain   = domain.ToString(),
            type     = type.ToString(),
            sub_type = subType.ToString(),
            key      = $"{domain}-{type}-{subType}"
        };
    }
}
