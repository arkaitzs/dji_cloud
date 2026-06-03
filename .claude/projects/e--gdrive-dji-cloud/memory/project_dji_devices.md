---
name: dji-device-camera-types
description: DJI Cloud API camera_index values per drone model — needed for correct live_start_push video_id
metadata:
  type: project
---

DJI Cloud API `camera_index` codes for `live_start_push` `video_id` field (`{sn}/{camera_index}/{video_index}`):

| Modelo | camera_index |
|--------|-------------|
| Mavic 3T (Thermal) | `67-0-0` |
| Mavic 3E (Enterprise) | `66-0-0` |
| Matrice 30 (M30) | `52-0-0` |
| Matrice 30T (M30T) | `53-0-0` |
| Zenmuse H20 | `42-0-0` |
| Zenmuse H20T | `43-0-0` |

**`camera_index: 0-0-0` is INVALID** — causes silent rejection by DJI Pilot 2, no services_reply.

**Why:** Confirmed during Mavic 3T live stream debugging. The RC subscribes to services topic but silently rejects live_start_push when camera_index is wrong. No services_reply is sent on rejection.

**How to apply:** Always use the `live_capacity` response from the drone to get the real camera_index. The server auto-requests live_capacity after update_topo handshake (`thing/product/{sn}/services` with method `live_capacity`). Check `/api/admin/live-capacity/{gatewaySn}`. Fall back to model-specific codes from this list. Never use `0-0-0`.

**Other DJI Cloud API fixes applied in this project:**
- `url_type = 1` for RTMP (not 0=Agora, not 2=RTMPS)
- `gateway` field required at top level of live_start_push payload
- `need_reply` should NOT be included in cloud→device commands
- Network probes (`sys/product/{sn}/network/probe`) must be sent periodically or DJI Pilot 2 may block streaming
- `live_capacity` must be auto-detected; UI camera selector must use real codes from drone

**User's setup:** Mavic 3T (SN: 1581F5FJD234L00DV2QX) + RC Pro (SN: 5YSZL3A0021UWX), Wi-Fi 192.168.1.x subnet.
