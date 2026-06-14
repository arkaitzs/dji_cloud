# Plan de migración a SQL Server Express

> Documento **vivo**: se actualiza a medida que avanzamos. La idea es pasar la
> persistencia de nuestro servidor (.NET) de **ficheros JSON + memoria** a
> **Microsoft SQL Server Express**, tomando como referencia el modelo de datos
> oficial de DJI Cloud API (MySQL), pero adaptado a nuestro stack y necesidades.

Última actualización: 2026-06-14

---

## 1. Estado actual de la persistencia (nuestro server .NET)

Hoy **NO usamos base de datos relacional**. Persistimos así:

| Dato | Dónde está hoy | Servicio |
|---|---|---|
| Elementos de mapa + grupos | `wwwroot/map_elements.json` (AtomicJsonFile) | `MapDataService` |
| Histórico de vuelos (track) | `wwwroot/flights/{id}.json` | `FlightRecorderService` |
| Rutas guardadas (WPML) | `wwwroot/routes/*.json` | `RouteStoreController` |
| Índice de waylines publicadas | JSON en disco | `DjiWaylineController` |
| Dispositivos / topología / estado | **memoria** (`ConcurrentDictionary`) | `AdminDataService` |
| Telemetría en vivo (OSD) | **efímera** (memoria → SignalR/WebSocket) | (no se persiste) |
| Pareja RC↔aeronave, gateways | memoria + cache JSON | `AdminDataService` |

**Principio que mantenemos:** la **telemetría en vivo NO va a la BD** (igual que DJI).
Solo se persiste el **histórico por vuelo** (track resumido) al aterrizar.

---

## 2. Modelo de datos objetivo en SQL Express

Tablas (basadas en el esquema oficial DJI `cloud_sample.sql` + nuestras extensiones).
Las **19 tablas DJI** de referencia: `manage_workspace`, `manage_user`,
`manage_device`, `manage_device_dictionary`, `manage_device_payload`,
`manage_device_hms`, `manage_device_firmware`, `manage_firmware_model`,
`manage_device_logs`, `logs_file`, `logs_file_index`, `map_group`,
`map_group_element`, `map_element_coordinate`, `device_flight_area`,
`flight_area_file`, `flight_area_property`, `wayline_file`, `wayline_job`,
`media_file`.

### Prioridad de migración (lo que usamos de verdad)
1. **Mapa** — `map_group`, `map_group_element`, `map_element_coordinate`
2. **Vuelos (extensión nuestra)** — `flight` (track) + enlace opcional a `wayline_job`
3. **Dispositivos / topología** — `manage_device` (+ `manage_device_dictionary`)
4. **Waylines** — `wayline_file`, `wayline_job`
5. **Workspaces / usuarios** — `manage_workspace`, `manage_user`
6. **Media / HMS / logs** — `media_file`, `manage_device_hms`, logs

### Tabla `flight` (nuestra — histórico de vuelos)
```sql
CREATE TABLE flight (
  id             VARCHAR(80)  NOT NULL PRIMARY KEY,
  drone_sn       VARCHAR(64),
  workspace_id   VARCHAR(64),
  wayline_job_id VARCHAR(64)  NULL,        -- enlace a la misión (si fue automática)
  start_time     DATETIME2,
  end_time       DATETIME2,
  frame_count    INT,
  distance_m     FLOAT,
  max_alt_m      FLOAT,
  track          NVARCHAR(MAX)              -- array JSON de puntos (= el .json actual)
);
CREATE INDEX ix_flight_drone_time ON flight(drone_sn, start_time);
```
- `track` como `NVARCHAR(MAX)` con el JSON de puntos = cambio mínimo (replica el fichero).
- Si se necesitan puntos consultables: tabla hija `flight_point(flight_id, ts, lat, lon, alt, …)`.

### Mapa (replicando DJI)
- `map_group` (group_id, name, group_type 0/1/2, is_distributed, is_lock, workspace_id)
- `map_group_element` (element_id, group_id, name, element_type, color, clamp_to_ground, username)
- `map_element_coordinate` (element_id, longitude, latitude, altitude) — **GeoJSON descompuesto en filas** (como DJI). El "Pilot Share Layer" debe llevar **UUID real + is_distributed=1** (clave del sync en tiempo real, ya resuelto en v0.03).

---

## 3. Equivalencias de tipos (MySQL/DJI → SQL Server)

| MySQL / DJI | SQL Server |
|---|---|
| `JSON` (TSL, resource) | `NVARCHAR(MAX)` + `JSON_VALUE`/`OPENJSON`/`ISJSON` |
| Coordenadas (lon/lat/alt) | `FLOAT`/`DECIMAL` en filas (portable) **o** tipo `geography` (espacial nativo) |
| `tinyint(1)` (bool) | `BIT` |
| `AUTO_INCREMENT` | `IDENTITY(1,1)` |
| `bigint` (timestamp ms) | `BIGINT` |
| `LIMIT n OFFSET m` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` |
| `ON DUPLICATE KEY UPDATE` | `MERGE` / upsert manual |
| `` `ident` `` | `[ident]` |

---

## 4. Acceso a datos en .NET

- **Driver:** `Microsoft.Data.SqlClient`.
- **ORM recomendado:** **EF Core** (`Microsoft.EntityFrameworkCore.SqlServer`) o Dapper para lo simple.
- **Connection string:** `Server=localhost\\SQLEXPRESS;Database=DjiCloud;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;`
- Configurable en `appsettings.json` (sección nueva `ConnectionStrings`).
- Migrar servicio a servicio detrás de las **interfaces existentes** (`IMapDataService`, `IFlightRecorderService`, `IAdminDataService`) → cambiar la implementación sin tocar el resto.

### Límites de SQL Express (a vigilar)
| Límite | Valor | Mitigación |
|---|---|---|
| Tamaño por BD | 10 GB | No persistir telemetría cruda; solo histórico por vuelo (decenas de miles caben) |
| RAM (buffer) | 1 GB | Suficiente para registro/metadatos |
| CPU | 1 socket / 4 cores | Suficiente |

---

## 5. Plan de migración por fases

- [ ] **Fase 0** — Decisión de ORM (EF Core vs Dapper) + crear `DjiCloudDbContext` + connection string en appsettings.
- [ ] **Fase 1** — Script T-SQL de creación de tablas (las prioritarias) + seed (grupos APP/Default con UUID real, workspace).
- [ ] **Fase 2** — Migrar **Mapa** (`MapDataService` → SQL). Mantener la interfaz `IMapDataService`. Importar el `map_elements.json` actual.
- [ ] **Fase 3** — Migrar **Vuelos** (`FlightRecorderService` → tabla `flight`). Importar los `flights/*.json`.
- [ ] **Fase 4** — Migrar **Dispositivos/topología** (`AdminDataService`) — ojo: parte es estado en vivo (puede seguir en memoria) + parte registro (a BD).
- [ ] **Fase 5** — Waylines, workspaces, usuarios, media.
- [ ] **Fase 6** — Quitar/*deprecate* los AtomicJsonFile reemplazados.

**Estrategia:** detrás de las interfaces actuales → migración incremental sin romper nada; doble escritura temporal (JSON + SQL) durante la transición si hace falta.

---

## 6. Registro de avances

> Añadir aquí cada paso completado, con fecha.

- **2026-06-14** — Documento creado. Decidido: telemetría en vivo efímera (memoria/SignalR);
  histórico de vuelos en tabla `flight` (track JSON), NO en `wayline_job` (esa es solo
  ejecución de misión). SQL Express viable porque no persistimos telemetría cruda.
