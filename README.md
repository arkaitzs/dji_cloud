# DJI Cloud Server

Servidor de control de flotas de drones DJI compatible con la **DJI Cloud API v1.11.x**. Implementa el protocolo *Pilot-to-Cloud* para recibir telemetría en tiempo real, gestionar misiones de waypoints, controlar streams de vídeo en directo y monitorizar el estado de la flota desde un mapa web interactivo.

---

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10 / ASP.NET Core (C#) |
| Broker MQTT | MQTTnet v4 (embebido) |
| Tiempo real | ASP.NET Core SignalR |
| Vídeo | FFmpeg → HLS · MediaMTX → WebRTC/RTMP |
| Frontend | HTML5 + Leaflet.js (mapa) + hls.js (player) |
| Instalador | Inno Setup 6 (servicio Windows) |

---

## Dispositivos compatibles

| Aeronave | Gateway | Protocolo |
|----------|---------|-----------|
| Matrice 4T / 4E | DJI RC Pro Enterprise + Pilot 2 | Pilot-to-Cloud |
| Mavic 3T / 3E | DJI RC Pro Enterprise + Pilot 2 | Pilot-to-Cloud |
| Matrice 30 / 30T | DJI RC Plus + Pilot 2 | Pilot-to-Cloud |
| Matrice 300 / 350 RTK | DJI RC Plus + Pilot 2 | Pilot-to-Cloud |
| Matrice 3D / 3TD | DJI Dock 2 | Dock-to-Cloud |
| Matrice 30 / 30T | DJI Dock | Dock-to-Cloud |

> **M4T**: el servidor detecta automáticamente el `type_code` y `sub_type` del M4T en el primer handshake MQTT y los registra en los logs para facilitar la identificación del dispositivo.

---

## Características principales

**Telemetría en tiempo real**
- OSD a 0.5 Hz en modo normal, hasta 30 Hz en modo DRC (Direct Remote Control)
- Posición GPS, altitud ASL y AGL, rumbo, actitud del gimbal y velocidad
- Estado de batería con tiempo de vuelo restante y umbrales de alarma
- 18 estados de vuelo del protocolo M3/M4 series (`mode_code`)
- Trayectoria coloreada por altitud con rango configurable

**Gestión de misiones**
- Subida de misiones `.kmz` (WPML) y envío al dron vía `flighttask_create`
- CRUD de rutas de waypoints persistentes en disco
- Registro de sesiones de vuelo con exportación a JSON, CSV y KML

**Streaming de vídeo**
- RTMP → HLS (FFmpeg) con latencia ~3 s
- WebRTC (WHIP/WHEP vía MediaMTX) con latencia < 500 ms
- Cambio de calidad en vuelo (`live_set_quality`) y cambio de lente (`live_lens_change`)
- Proxy WHIP integrado para compatibilidad con el firmware DJI

**Mapa interactivo**
- Marcador del dron con burbuja de altitud y referencia ASL/AGL seleccionable
- Selector de referencia de altitud (ASL / AGL) con recoloreo inmediato de trayectorias
- Badge global que refleja el estado del dron seleccionado
- Restauración automática del mapa tras reconexión SignalR
- Timeout de offline unificado entre servidor y frontend (15 s)

**Comandos de vuelo**
- Return to Home (RTH) y cancelación
- Modo DRC con resolución automática de IP en entornos multi-homed
- Envío de misiones `.kmz` y control de cámara

---

## Arquitectura

```
DJI Pilot 2 / RC Pro
        │
        │  MQTT (1883)
        ▼
┌───────────────────────────────────────┐
│         DjiCloudServer (.NET 10)       │
│                                        │
│  ┌─────────────┐  ┌────────────────┐  │
│  │ MQTT Broker │  │  Kestrel HTTP  │  │
│  │ (MQTTnet)   │  │  (puerto 5072) │  │
│  └──────┬──────┘  └───────┬────────┘  │
│         │                 │           │
│  ┌──────▼─────────────────▼────────┐  │
│  │        Program.cs               │  │
│  │  (interceptor de mensajes MQTT) │  │
│  └─────────────────┬───────────────┘  │
│                    │                   │
│  ┌─────────────────▼───────────────┐  │
│  │  AdminDataService (singleton)   │  │
│  │  TrajectoryStore · FlightRec    │  │
│  └─────────────────┬───────────────┘  │
│                    │  SignalR          │
│  ┌─────────────────▼───────────────┐  │
│  │         TelemetryHub            │  │
│  └─────────────────────────────────┘  │
└───────────────────────────────────────┘
        │
        │  WebSocket (SignalR)
        ▼
   map.html (Leaflet + hls.js)
```

---

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- `ffmpeg.exe` en el PATH del sistema o en la carpeta de publicación
- `mediamtx.exe` en la misma carpeta que el ejecutable (para streaming WebRTC)
- Credenciales DJI: AppId, AppKey y License del [portal de desarrolladores](https://developer.dji.com)
- Windows 10/11 para el instalador como servicio. Linux compatible en modo manual.

---

## Instalación y arranque

### Desarrollo local

```bash
git clone https://github.com/arkaitzs/dji_cloud.git
cd dji_cloud

# Copiar la plantilla de configuración y rellenar las credenciales DJI
cp src/DjiCloudServer/appsettings.Development.json src/DjiCloudServer/appsettings.json

dotnet restore src/DjiCloudSolution.slnx
dotnet run --project src/DjiCloudServer/DjiCloudServer.csproj --urls "http://0.0.0.0:5072"
```

### Producción (servicio Windows)

```powershell
# Publica la aplicación autocontenida y genera el instalador
.\publish.ps1
# El instalador registra el servicio Windows y copia ffmpeg.exe y mediamtx.exe
```

---

## Configuración

Crea `src/DjiCloudServer/appsettings.json` a partir de la plantilla de desarrollo:

```json
{
  "DjiCloud": {
    "AppId":    "<tu-app-id>",
    "AppKey":   "<tu-app-key>",
    "License":  "<tu-licencia-dji>",
    "ServerIp": "",
    "Mqtt": {
      "Host":     "localhost",
      "Port":     1883,
      "Username": "",
      "Password": "",
      "ClientId": "DjiCloudServer"
    }
  }
}
```

| Campo | Descripción |
|-------|-------------|
| `AppId` / `AppKey` / `License` | Credenciales del portal de desarrolladores DJI. **No subir al repositorio.** |
| `ServerIp` | IP fija del servidor en la LAN del dron. Si está vacío, se resuelve automáticamente por subred. |
| `Mqtt.Port` | Puerto del broker MQTT embebido (default: 1883). |

> `appsettings.json` está en `.gitignore`. Las credenciales nunca deben commitearse.

---

## Endpoints REST principales

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/health` | Estado del servidor y versión |
| `GET` | `/api/admin/active-drones` | Drones online con estado completo |
| `GET` | `/api/admin/drone-state/{sn}` | Estado estructurado de un dron específico |
| `GET` | `/api/admin/gateways` | Gateways (RC / Dock) y aeronaves pareadas |
| `GET` | `/api/admin/hms/{sn}` | Códigos HMS activos del dron |
| `GET` | `/api/admin/logs` | Log de eventos del servidor |
| `GET` | `/api/flights` | Historial de sesiones de vuelo |
| `GET` | `/api/flights/{id}/export?format=csv\|kml\|json` | Exportar sesión |
| `POST` | `/api/waypoints/save` | Subir misión `.kmz` |
| `POST` | `/api/waypoints/send/{sn}` | Enviar misión al dron |
| `POST` | `/api/drone-commands/return-home/{gatewaySn}` | Ordenar RTH |
| `POST` | `/api/stream/start-live` | Iniciar stream RTMP o WebRTC |
| `POST` | `/api/stream/stop-live/{gatewaySn}` | Detener stream |
| `POST` | `/api/drc/enter/{gatewaySn}` | Activar modo DRC (telemetría alta frecuencia) |
| `GET` | `/api/config/network` | IPs disponibles del servidor |
| `POST` | `/api/config/network` | Fijar ServerIp en appsettings.json |

Documentación interactiva Swagger en `http://<servidor>:5072/swagger`.

---

## Interfaces web

| URL | Descripción |
|-----|-------------|
| `/map.html` | Mapa de control de flota en tiempo real |
| `/dashboard.html` | Panel de administración: logs, dispositivos, streams |
| `/index.html` | Vista H5 para DJI Pilot 2 (se carga en el mando RC) |

---

## Estructura del repositorio

```
dji_cloud/
├── src/
│   └── DjiCloudServer/
│       ├── Controllers/      # API REST (Admin, Stream, DRC, Waypoint, Flight…)
│       ├── Hubs/             # TelemetryHub (SignalR)
│       ├── Models/           # DTOs y modelos de datos
│       ├── Services/         # AdminDataService, FfmpegService, FlightRecorder…
│       ├── Program.cs        # Configuración + interceptor MQTT central
│       └── wwwroot/          # Frontend (map.html, dashboard.html, index.html)
├── installer/                # Script Inno Setup para el instalador Windows
├── mediamate_server/         # Configuración de MediaMTX (WebRTC/RTMP)
└── publish.ps1               # Script de publicación y empaquetado
```

---

## Ramas

| Rama | Descripción |
|------|-------------|
| `main` | Rama principal |
| `v00` | Versión estable en producción |
| `feature/m4t-cloud-api-fixes` | Compatibilidad M4T + mejoras de AdminController y mapa |

---

## Licencia

Proyecto propietario — USBA Sotomayor. Uso interno.
