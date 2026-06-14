# DJI Cloud Server

Servidor propio de **DJI Cloud API v1.11.x** (ASP.NET Core / .NET 10) que conecta
drones DJI y mandos **DJI Pilot 2** con un **mapa táctico web**, sin depender de
la nube de DJI.

## ¿Qué hace?

- **Mapa táctico web** (`/map.html`): telemetría en vivo del dron (posición,
  batería, GPS, gimbal), dibujo de elementos y zonas.
- **Streaming de vídeo en vivo** del dron en el mapa web (DRC / HLS), con la cámara
  seleccionada según la capacidad del aparato (`live_capacity`).
- **Sincronización BIDIRECCIONAL de elementos de mapa en TIEMPO REAL** entre la web
  y los mandos DJI Pilot 2 (Web ↔ mando ↔ mando). Dibuja en el mando y aparece al
  instante en la web y en los demás mandos, y viceversa. _(Resuelto en v0.03: la
  capa "Pilot Share Layer" debe tener UUID real + `is_distributed=1`, como el seed
  oficial de DJI — el zero-UUID hacía que Pilot solo subiera en batch.)_
- **Dibujo y edición de elementos desde la web** (`map.html`): puntos, líneas,
  polígonos y círculos; edición interactiva (arrastrar la figura, mover vértices,
  redimensionar el círculo — vía Leaflet-Geoman), paleta de colores DJI, y borrado.
  Todo se propaga a los mandos en tiempo real.
- **Planificador de rutas WPML** (botón "Ruta"): genera misiones `.kmz` para
  cualquier modelo de dron (M3T/M3E/M3M, M4T/M4E, M30/M30T, M350…), con acciones
  de cámara (foto, orientación de gimbal, hover) por waypoint.
- **Broker MQTT embebido** (no necesitas Mosquitto): el dron y el mando se conectan
  directamente a este servidor.
- **Topología de dispositivos**: reconoce mandos (gateway) y sus drones asociados.
- **Panel de administración** (`/index.html`): dispositivos, estado, logs.

## Requisitos

- **Windows 10/11** (se ejecuta como aplicación o como servicio de Windows).
- **.NET 10 SDK** (para compilar) o **.NET 10 Runtime** (para ejecutar el publicado).
- **Cuenta de desarrollador DJI** con Cloud API habilitada → necesitas `AppId`,
  `AppKey` y `License` desde <https://developer.dji.com/>
- Dron/mando y servidor en la **misma red local (LAN)**.

## Configuración

Toda la configuración está en `src/DjiCloudServer/appsettings.json`. Copia la
plantilla y rellena tus valores:

```powershell
copy src\DjiCloudServer\appsettings.example.json src\DjiCloudServer\appsettings.json
```

| Clave                  | Qué es / dónde se obtiene                                              |
|------------------------|------------------------------------------------------------------------|
| `DjiCloud:AppId`       | ID de tu app en el portal de desarrollador DJI                         |
| `DjiCloud:AppKey`      | Clave de tu app DJI **(secreta — no la subas a git)**                  |
| `DjiCloud:License`     | Licencia de la app DJI (cadena larga base64)                           |
| `DjiCloud:ServerIp`    | **IP LAN de este PC** que el dron puede alcanzar (ej. `192.168.1.150`) |
| `DjiCloud:Mqtt:Host`   | Host del broker MQTT — normalmente `localhost`                         |
| `DjiCloud:Mqtt:Port`   | Puerto MQTT — `1883`                                                    |
| `DjiCloud:WorkspaceId` | Workspace por defecto (deja el valor de la plantilla salvo que sepas cambiarlo) |

> **`ServerIp` es la clave del éxito**: tiene que ser la IP de tu PC en la red del
> dron. Compruébala con `ipconfig`. Si está mal, el vídeo en vivo y el DRC fallan.

## Puertos (abrir en el firewall de Windows)

| Puerto | Uso                                                  |
|--------|------------------------------------------------------|
| `5072` | Web (mapa + panel admin) y API HTTP                  |
| `1883` | Broker MQTT (conexión del dron y del mando)          |
| `7079` | HTTPS (solo perfil de desarrollo)                    |

Para cambiar los puertos en producción, define `ASPNETCORE_URLS`
(ej. `http://0.0.0.0:8080;https://0.0.0.0:8443`).

## Configurar DJI Pilot 2 (en el mando)

1. En el mando, abre Pilot 2 → ajustes de **Cloud API**.
2. Apunta a este servidor: `http://<ServerIp>:5072` (o tu URL HTTPS).
3. Introduce las credenciales de tu app DJI.
4. Conecta — el mando aparecerá en el panel de administración.

## Ejecutar

**Desarrollo:**

```powershell
cd src\DjiCloudServer
dotnet run
```

Abre <http://localhost:5072/map.html>

**Producción (publicar + servicio de Windows):**

```powershell
dotnet publish src\DjiCloudServer -c Release -o C:\DjiCloudServer
sc.exe create DjiCloudServer binPath= "C:\DjiCloudServer\DjiCloudServer.exe" start= auto
sc.exe start DjiCloudServer
```

## Seguridad

- **Nunca** subas `appsettings.json` con credenciales reales — está en `.gitignore`.
- La carpeta `pilot_log_keys/` (clave privada RSA de descifrado de logs) está
  ignorada y no debe publicarse.

## Versión

### v0.03 (actual)
Sincronización de elementos de mapa **bidireccional y en tiempo real** RC ↔ Web ↔ RC,
y **edición completa** desde la web.

- **Fix raíz de la sincronización:** la capa Pilot Share Layer ahora usa **UUID real**
  (`e3dea0f5-…-3228060`) + `is_distributed=1`, como el servidor oficial de DJI. Con el
  zero-UUID anterior, Pilot trataba la capa como local y solo subía en batch al
  reconectar. Verificado contra el servidor oficial: ahora sube/baja al instante.
- **Dibujo web:** punto, línea, polígono y círculo, con paleta de colores DJI.
- **Edición interactiva** (Leaflet-Geoman): arrastrar figuras, mover vértices,
  redimensionar círculos, recolorear y borrar — todo se propaga a los mandos.
- **TSA:** distribución periódica de posiciones (aeronaves + mandos) entre mandos.
- **Multi-vista de vídeo** (`/multiview.html`): varias cámaras a la vez.
- El mando re-descarga todos los elementos del servidor al conectar (incl. 1ª vez).

### v0.1.0
Primera versión instalable: sincronización RC↔Web de elementos de mapa (Web→RC),
rutas WPML con acciones de cámara, telemetría y vídeo en vivo, panel de administración.
