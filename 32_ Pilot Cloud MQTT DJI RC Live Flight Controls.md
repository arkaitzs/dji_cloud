# Live Flight Controls

2026-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/00.mqtt/20.rc-pro/30.drc.md) 

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#event)Event

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#authorization-result-notification)Authorization result notification

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** cloud_control_auth_notify

**Data:**

| Column  | Name        | Type        | constraint                                                                                                                                    | Description                  |
| ------- | ----------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| result  | Return code | int         |                                                                                                                                               | Non-zero represents an error |
| output  | Output      | struct      |                                                                                                                                               |                              |
| »status | Task state  | enum_string | {"canceled":"Another user initiated an authorization request, this request was canceled.","failed":"Error or user denied","ok":"User agreed"} |                              |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "output": {
            "status": "ok"
        },
        "result": 0
    },
    "method": "cloud_control_auth_notify",
    "need_reply": 0,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1704038400000
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#drc-link-state-notification-abandoned)DRC link state notification (Abandoned)

**Note: This protocol is no longer maintained and may be inaccurate. For more accurate DRC link status, use the device attribute “drc_state” reported by the remote controller or the “DRC-heart beat”.**

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** drc_status_notify

**Data:**

| Column    | Name                       | Type     | constraint                                             | Description                  |
| --------- | -------------------------- | -------- | ------------------------------------------------------ | ---------------------------- |
| result    | Return code                | int      |                                                        | Non-zero represents an error |
| drc_state | Live flight controls state | enum_int | {"0":"Not connected","1":"Connecting","2":"Connected"} |                              |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "drc_state": 2,
        "result": 0
    },
    "need_reply": 1,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#report-photo-capturing-progress)Report photo capturing progress

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** camera_photo_take_progress

**Data:**

| Column         | Name                   | Type        | constraint                                                                                                                                               | Description                  |
| -------------- | ---------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| output         | Output                 | struct      |                                                                                                                                                          |                              |
| »status        | Photo capturing status | enum_string | {"fail":"Failed","in_progress":"Executing","ok":"Finished"}                                                                                              |                              |
| »progress      | Progress               | struct      |                                                                                                                                                          |                              |
| »»current_step | Execution step         | enum_int    | {"3000":"Capturing panorama has not started or is finished","3002":"Panorama photo capturing in progress","3005":"Panorama photo stitching in progress"} |                              |
| »»percent      | Progress value         | int         | {"max":"100","min":"0","step":"1"}                                                                                                                       |                              |
| »ext           | Extended content       | struct      |                                                                                                                                                          |                              |
| »»camera_mode  | Current camera mode    | enum_int    | {"3":"Panoramic photography"}                                                                                                                            |                              |
| result         | Return code            | int         |                                                                                                                                                          | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "output": {
            "ext": {
                "camera_mode": 3
            },
            "progress": {
                "current_step": 0,
                "percent": 100
            },
            "status": "ok"
        },
        "result": 0
    },
    "need_reply": 1,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#service)Service

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#release-cloud-control)Release cloud control

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** cloud_control_release

**Data:**

| Column       | Name             | Type  | constraint                     | Description                                                                    |
| ------------ | ---------------- | ----- | ------------------------------ | ------------------------------------------------------------------------------ |
| control_keys | Control key list | array | {"size": 1, "item_type": text} | List of control keys to release. "flight" represents flight control authority. |

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** cloud_control_release

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "output": {
            "status": "ok"
        },
        "result": 0
    },
    "method": "cloud_control_release",
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1704038400000
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#request-cloud-control-authorization)Request cloud control authorization

After the request, a "Request Authorization" pop-up will appear on the remote controller.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** cloud_control_auth_request

**Data:**

| Column        | Name          | Type  | constraint                       | Description                                                       |
| ------------- | ------------- | ----- | -------------------------------- | ----------------------------------------------------------------- |
| user_id       | User ID       | text  | { "length": "" }                 | The user ID of the cloud user requesting authorization.           |
| user_callsign | User Callsign | text  | { "length": "" }                 | The nickname of the cloud user requesting authorization.          |
| control_keys  | Control Keys  | array | { "size": 1, "item_type": text } | To request flight control authorization, please fill in "flight". |

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** cloud_control_auth_request

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "output": {
            "status": "ok"
        },
        "result": 0
    },
    "method": "cloud_control_auth_request",
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1704038400000
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#enter-live-flight-controls-mode)Enter live flight controls mode

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** drc_mode_enter

**Data:**

| Column        | Name                                       | Type   | constraint                                            | Description                                                                                                                                                                           |
| ------------- | ------------------------------------------ | ------ | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| mqtt_broker   | Broker connection information              | struct |                                                       | Get the address and authentication information of the MQTT relay service                                                                                                              |
| »address      | Server connection address                  | text   |                                                       | Server connection address, for example, 192.0.2.1:8883, mqtt.dji.com:8883                                                                                                             |
| »client_id    | Client ID                                  | text   |                                                       | Customizable MQTT client ID. It is recommended to use the device's serial number (SN code). It can also be combined with a meaningful prefix, such as `drc-4J4R101`                   |
| »username     | User name                                  | text   |                                                       | Username used when establishing a connection                                                                                                                                          |
| »password     | Password                                   | text   |                                                       | Password required for authentication when establishing a connection                                                                                                                   |
| »expire_time  | Authentication information expiration time | int    | {"unit_name":"Seconds / s"}                           | Authentication information can be reused within the validity period. The expiration of authentication information does not affect devices that have already established a connection. |
| »enable_tls   | Whether to enable TLS                      | bool   |                                                       | Enable TLS, i.e., encrypt the MQTT link                                                                                                                                               |
| osd_frequency | OSD frequency                              | int    | {"max":30,"min":1,"step":"","unit_name":"Hertz / Hz"} | Set OSD reporting frequency                                                                                                                                                           |
| hsi_frequency | HSI frequency                              | int    | {"max":30,"min":1,"step":"","unit_name":"Hertz / Hz"} | Set HSI reporting frequency                                                                                                                                                           |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "hsi_frequency": 1,
        "mqtt_broker": {
            "address": "mqtt.dji.com:8883",
            "client_id": "sn_a",
            "enable_tls": true,
            "expire_time": 1672744922,
            "password": "jwt_token",
            "username": "sn_a_username"
        },
        "osd_frequency": 10
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** drc_mode_enter

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#exit-live-flight-controls-mode)Exit live flight controls mode

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** drc_mode_exit

**Data:** null

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {},
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "drc_mode_exit"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** drc_mode_exit

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "drc_mode_exit"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-switch-camera-mode)Payload control - Switch Camera Mode

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_mode_switch

**Data:**

| Column        | Name               | Type     | constraint                                                                          | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | -------- | ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text     |                                                                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_mode   | Camera mode        | enum_int | {"0":"Capturing","1":"Recording","2":"Smart Low-Light","3":"Panoramic photography"} |                                                                                                                                                                                                                          |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_mode": 0,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_mode_switch

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-start-recording)Payload control - Start Recording

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_recording_start

**Data:**

| Column        | Name               | Type | constraint | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text |            | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_recording_start

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-stop-recording)Payload control - Stop Recording

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_recording_stop

**Data:**

| Column        | Name               | Type | constraint | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text |            | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_recording_stop

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-screen-drag-control)Payload control - Screen Drag Control

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_screen_drag

**Data:**

| Column        | Name                                                                                      | Type   | constraint                                                                                                                                          | Description                                                                                                                                                                                                              |
| ------------- | ----------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration                                                                        | text   |                                                                                                                                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool   | {"false":"Only gimbal turns, the aircraft body does not turn","true":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |                                                                                                                                                                                                                          |
| pitch_speed   | Gimbal pitch speed                                                                        | double | {"unit_name":"Degree per second / degree/s"}                                                                                                        | Gimbal pitch speed                                                                                                                                                                                                       |
| yaw_speed     | Gimbal yaw speed                                                                          | double | {"unit_name":"Degree per second / degree/s"}                                                                                                        | Gimbal yaw speed, effective only when the aircraft head is not locked.                                                                                                                                                   |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "locked": true,
        "payload_index": "39-0-7",
        "pitch_speed": 0.1,
        "yaw_speed": 0.1
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_screen_drag

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-double-tap-to-aim)Payload control - Double Tap to AIM

The double-tap AIM function allows you to double-tap on a target point within the camera's field of view, making that point the center of the camera's field of view.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_aim

**Data:**

| Column        | Name                                                                                      | Type        | constraint                                                                                                                                          | Description                                                                                                                                                                                                              |
| ------------- | ----------------------------------------------------------------------------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration                                                                        | text        |                                                                                                                                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type   | Camera type                                                                               | enum_string | {"ir":"Infrared","wide":"Wide-angle","zoom":"Zoom"}                                                                                                 | Camera type enumeration                                                                                                                                                                                                  |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool        | {"false":"Only gimbal turns, the aircraft body does not turn","true":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |                                                                                                                                                                                                                          |
| x             | Target coordinates x                                                                      | double      | {"max":1,"min":0}                                                                                                                                   | Target coordinates x, with the upper-left corner of the lens as the coordinate center, and the horizontal direction as x.                                                                                                |
| y             | Target coordinates y                                                                      | double      | {"max":1,"min":0}                                                                                                                                   | Target coordinates y, with the upper-left corner of the lens as the coordinate center, and the vertical direction as y.                                                                                                  |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "locked": true,
        "payload_index": "39-0-7",
        "x": 0.5,
        "y": 0.5
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_aim

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-zoom)Payload control - Zoom

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_focal_length_set

**Data:**

| Column        | Name               | Type        | constraint                                          | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ----------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text        |                                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type   | Camera type        | enum_string | {"ir":"Infrared","wide":"Wide-angle","zoom":"Zoom"} | Camera type enumeration                                                                                                                                                                                                  |
| zoom_factor   | Zoom factor        | double      | {"max":200,"min":2}                                 | Zoom factor, 2-200 for visible light, 2-20 for infrared.                                                                                                                                                                 |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "payload_index": "39-0-7",
        "zoom_factor": 5
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_focal_length_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-reset-gimbal)Payload control - Reset gimbal

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** gimbal_reset

**Data:**

| Column        | Name            | Type     | constraint                                                                         | Description                                                                                                                                                                                                                             |
| ------------- | --------------- | -------- | ---------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Payload index   | text     |                                                                                    | Payload index. Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| reset_mode    | Reset mode type | enum_int | {"0":"Reset","1":"Downward","2":"Gimbal pan recenter","3":"Gimbal pitch downward"} |                                                                                                                                                                                                                                         |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7",
        "reset_mode": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** gimbal_reset

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-look-at)Payload control - Look At

The Look At function instructs the aircraft to turn from its current orientation to a point specified by actual latitude, longitude, and altitude. For the M30/M30T model, it is recommended to use the method of locking the head. An exception may occur if the gimbal rotates only when the gimbal limit angle is reached.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_look_at

**Data:**

| Column        | Name                                                                                      | Type   | constraint                                                                                                                                          | Description                                                                                                                                                                                                              |
| ------------- | ----------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration                                                                        | text   |                                                                                                                                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool   | {"false":"Only gimbal turns, the aircraft body does not turn","true":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |                                                                                                                                                                                                                          |
| latitude      | Target point latitude (in degree values)                                                  | double | {"max":90,"min":-90}                                                                                                                                | Angle value. Negative for south latitude, positive for north latitude, accurate to 6 decimal places.                                                                                                                     |
| longitude     | Target point longitude (in degree values)                                                 | double | {"max":180,"min":-180}                                                                                                                              | Angle value. East longitude is positive, west longitude is negative, precision to 6 decimal places.                                                                                                                      |
| height        | Target point height                                                                       | float  | {"max":10000,"min":2,"step":0.1,"unit_name":"Meters / m"}                                                                                           | Target point height (ellipsoid height)                                                                                                                                                                                   |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "height": 100,
        "latitude": 12.23,
        "locked": true,
        "longitude": 12.23,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_look_at

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-split-screen)Payload control - Split Screen

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_screen_split

**Data:**

| Column        | Name                           | Type | constraint | Description                                                                                                                                                                                                              |
| ------------- | ------------------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration             | text |            | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| enable        | Whether to enable split screen | bool |            | Enable or disable split screen                                                                                                                                                                                           |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "enable": true,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_screen_split

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-photo-storage-settings)Payload control - Photo storage settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** photo_storage_set

**Data:**

| Column                 | Name                              | Type  | constraint                            | Description                                                                                                                                                                                                              |
| ---------------------- | --------------------------------- | ----- | ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index          | Camera enumeration                | text  |                                       | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| photo_storage_settings | Photo storage settings collection | array | {"size": -, "item_type": enum_string} | Photo storage types {current, vision, ir}, can be multiple                                                                                                                                                               |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7",
        "photo_storage_settings": [
            "current",
            "vision",
            "ir"
        ]
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** photo_storage_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-video-storage-settings)Payload control - Video storage settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** video_storage_set

**Data:**

| Column                 | Name                                 | Type  | constraint                            | Description                                                                                                                                                                                                              |
| ---------------------- | ------------------------------------ | ----- | ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index          | Camera enumeration                   | text  |                                       | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| video_storage_settings | Collection of video storage settings | array | {"size": -, "item_type": enum_string} | Video storage types {current, vision, ir}, can be selected multiple                                                                                                                                                      |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7",
        "video_storage_settings": [
            "current",
            "vision",
            "ir"
        ]
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** video_storage_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-subject-zoom)Payload control - subject zoom

The subject zoom function enables to select and frame a target area within the camera's field of view. When activated, the camera screen will automatically switch to the target frame, zoom in and adjust the gimbal to the center.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_frame_zoom

**Data:**

| Column        | Name                                                                                      | Type        | constraint                                                                                                                                   | Description |
| ------------- | ----------------------------------------------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| payload_index | Camera enumeration                                                                        | text        |                                                                                                                                              |             |
| camera_type   | Camera type                                                                               | enum_string | {"ir":"Infrared","wide":"Wide-angle","zoom":"Zoom"}                                                                                          |             |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool        | {"0":"Only gimbal turns, the aircraft body does not turn","1":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |             |
| x             | Upper-left coordinate x of the target frame                                               | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |
| y             | Upper-left coordinate y of the target frame                                               | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |
| width         | Width of the target frame                                                                 | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |
| height        | Height of target frame                                                                    | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "height": 0.2,
        "locked": true,
        "payload_index": "39-0-7",
        "width": 0.2,
        "x": 0.5,
        "y": 0.5
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_frame_zoom

**Data:**

| Column | Name        | Type | constraint | Description |
| ------ | ----------- | ---- | ---------- | ----------- |
| result | Return code | int  |            |             |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-infrared-temperature-area-settings)Payload control - infrared temperature area settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** ir_metering_area_set

**Data:**

| Column        | Name                                                   | Type   | constraint                               | Description                                                                                                                                                                                                              |
| ------------- | ------------------------------------------------------ | ------ | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration                                     | text   |                                          | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| x             | Temperature area coordinate x of the upper left corner | double | {"max":1,"min":"","step":"","unit_name"} | Horizontal direction x with the upper left corner of the lens as the coordinate center                                                                                                                                   |
| y             | Temperature area coordinate y of the upper left corner | double | {"max":1,"min":"","step":"","unit_name"} | Vertical direction y with the upper left corner of the lens as the coordinate center                                                                                                                                     |
| width         | Width of the temperature area                          | double | {"max":1,"min":"","step":"","unit_name"} |                                                                                                                                                                                                                          |
| height        | Height of the temperature area                         | double | {"max":1,"min":"","step":"","unit_name"} |                                                                                                                                                                                                                          |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "height": 0.5,
        "payload_index": "39-0-7",
        "width": 0.5,
        "x": 0.5,
        "y": 0.5
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** ir_metering_area_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-infrared-temperature-point-settings)Payload control - infrared temperature point settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** ir_metering_point_set

**Data:**

| Column        | Name                           | Type   | constraint                                    | Description                                                                                                                                                                                                              |
| ------------- | ------------------------------ | ------ | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration             | text   |                                               | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| x             | Temperature point coordinate x | double | {"max":1,"min":"","step":"","unit_name":null} | Coordinates x of temperature point with the upper left corner of the lens as the coordinate center, horizontal direction as x                                                                                            |
| y             | Temperature point coordinate y | double | {"max":1,"min":"","step":"","unit_name":null} | Coordinates y of temperature point with the upper left corner of the lens as the coordinate center, vertical direction as y                                                                                              |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7",
        "x": 0.5,
        "y": 0.5
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** ir_metering_point_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-infrared-temperature-mode-settings)Payload control - infrared temperature mode settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** ir_metering_mode_set

**Data:**

| Column        | Name               | Type     | constraint                                                                                                      | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | -------- | --------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text     |                                                                                                                 | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| mode          | Temperature mode   | enum_int | {"0":"Turn off temperature measurement","1":"Point temperature measurement","2":"Area temperature measurement"} |                                                                                                                                                                                                                          |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "mode": 1,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** ir_metering_mode_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-spot-af)Payload control - Spot AF

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_point_focus_action

**Data:**

| Column        | Name                     | Type        | constraint                                    | Description                                                                                                                                                                                                              |
| ------------- | ------------------------ | ----------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration       | text        |                                               | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type   | Camera type              | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}           | Camera type enumeration. Note: This parameter is only supported under the zoom lens configuration for Matrice 30 series aircraft.                                                                                        |
| x             | Focus point coordinate x | double      | {"max":1,"min":"","step":"","unit_name":null} | Coordinate x with the upper left corner of the lens as the origin, horizontal direction.                                                                                                                                 |
| y             | Focus point coordinate y | double      | {"max":1,"min":"","step":"","unit_name":null} | Coordinate y with the upper left corner of the lens as the origin, vertical direction.                                                                                                                                   |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "payload_index": "39-0-7",
        "x": 0.5,
        "y": 0.5
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_point_focus_action

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-camera-focus-value-settings)Payload control - camera focus value settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_focus_value_set

**Data:**

| Column        | Name               | Type        | constraint                          | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ----------- | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text        |                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type   | Camera type        | enum_string | {"wide":"Wide-angle","zoom":"Zoom"} | Camera type enumeration. Note: This parameter is only supported under the zoom lens configuration for Matrice 30 series aircraft.                                                                                        |
| focus_value   | Focus value        | int         |                                     | Focus value, range refer to aircraft model attributes zoom_max_focus_value and zoom_min_focus_value.                                                                                                                     |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "focus_value": 5,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_focus_value_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-camera-focus-mode-settings)Payload control - camera focus mode settings

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_focus_mode_set

**Data:**

| Column        | Name               | Type        | constraint                          | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ----------- | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text        |                                     | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type   | Camera type        | enum_string | {"wide":"Wide-angle","zoom":"Zoom"} | Camera type enumeration. Note: The Matrice 30 series aircraft only supports configuring this parameter under the zoom lens.                                                                                              |
| focus_mode    | Focus mode         | enum_int    | {"0":"MF","1":"AFS","2":"AFC"}      |                                                                                                                                                                                                                          |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "focus_mode": 1,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_focus_mode_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-camera-exposure-value-adjustment)Payload control - camera exposure value adjustment

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_exposure_set

**Data:**

| Column         | Name               | Type        | constraint                                                                                                                                                                                                                                                                                                                                                                                                                             | Description                                                                                                                                                                                                              |
| -------------- | ------------------ | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index  | Camera enumeration | text        |                                                                                                                                                                                                                                                                                                                                                                                                                                        | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type    | Camera type        | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                                                                                                                                                                                                                                                                                                                                                                                    |                                                                                                                                                                                                                          |
| exposure_value | Exposure value     | enum_string | {"1":"-5.0EV","2":"-4.7EV","3":"-4.3EV","4":"-4.0EV","5":"-3.7EV","6":"-3.3EV","7":"-3.0EV","8":"-2.7EV","9":"-2.3EV","10":"-2.0EV","11":"-1.7EV","12":"-1.3EV","13":"-1.0EV","14":"-0.7EV","15":"-0.3EV","16":"0EV","17":"0.3EV","18":"0.7EV","19":"1.0EV","20":"1.3EV","21":"1.7EV","22":"2.0EV","23":"2.3EV","24":"2.7EV","25":"3.0EV","26":"3.3EV","27":"3.7EV","28":"4.0EV","29":"4.3EV","30":"4.7EV","31":"5.0EV","255":"FIXED"} |                                                                                                                                                                                                                          |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "exposure_value": 5,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_exposure_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-camera-exposure-mode-adjustment)Payload control - camera exposure mode adjustment

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_exposure_mode_set

**Data:**

| Column        | Name               | Type        | constraint                                                               | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ----------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text        |                                                                          | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |
| camera_type   | Camera type        | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                      | Camera type enumeration                                                                                                                                                                                                  |
| exposure_mode | Exposure Mode      | enum_int    | {"1":"Auto","2":"Shutter Priority","3":"Aperture Priority","4":"Manual"} |                                                                                                                                                                                                                          |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "camera_type": "zoom",
        "exposure_mode": 1,
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_exposure_mode_set

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-stop-capturing)Payload control - stop capturing

Stop capturing command, currently only panorama capture mode is supported.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_photo_stop

**Data:**

| Column        | Name               | Type | constraint | Description                                                                                                                                                                                                              |
| ------------- | ------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index | Camera enumeration | text |            | Camera enumeration values. Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_photo_stop

**Data:**

| Column | Name        | Type | constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#payload-control-start-capturing)Payload control - start capturing

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** camera_photo_take

**Data:**

| Column        | Name               | Type | constraint | Description                                                                                                                                                                                   |
| ------------- | ------------------ | ---- | ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text |            | Non-standard device_mode_key, formatted as {type-subtype-gimbalindex}. Please refer to [Product Supported](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "payload_index": "39-0-7"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** camera_photo_take

**Data:**

| Column | Name        | Type        | constraint                  | Description                                                                                                                                                                                                                         |
| ------ | ----------- | ----------- | --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| result | Return code | int         |                             | Non-zero represents an error                                                                                                                                                                                                        |
| status | Task state  | enum_string | {"in_progress":"Executing"} | When capturing panoramas or other continuous photography actions, status information will be reported. Expect ongoing progress events to be reported. For detailed content, please refer to the `camera_photo_take_progress` event. |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "output": {
            "status": "in_progress"
        },
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#drc)DRC

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#drc-heartbeat)DRC - Heartbeat

The `seq` at the same level as `data` is an incrementing sequence number that ensures the sequential execution of commands.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** heart_beat

**Data:**

| Column    | Name                                | Type | constraint                        | Description                                                                                                                                                                                                                                                                                                                                          |
| --------- | ----------------------------------- | ---- | --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| seq       | [Deprecated]Command sequence number | int  |                                   | Incremental sequence number to ensure the sequential execution of commands                                                                                                                                                                                                                                                                           |
| timestamp | Heartbeat transmission timestamp    | int  | {"unit_name":"Milliseconds / ms"} | The `heart_beat` protocol can determine whether the DRC link is active or not. If no heartbeat response is received within a certain period, the DRC link can be considered abnormal, and a retry can be initiated. If no heartbeat protocol is received for more than one minute, the device will consider the DRC link idle and exit the DRC link. |

**Example:**

```
{
    "data": {
        "timestamp": 1670415891013
    },
    "method": "heart_beat",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** heart_beat

**Data:**

| Column    | Name                                | Type | constraint                        | Description                                                                                                                                                                                                                                                                                                                                          |
| --------- | ----------------------------------- | ---- | --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| seq       | [Deprecated]Command sequence number | int  |                                   | Incremental sequence number to ensure the sequential execution of commands                                                                                                                                                                                                                                                                           |
| timestamp | Heartbeat transmission timestamp    | int  | {"unit_name":"Milliseconds / ms"} | The `heart_beat` protocol can determine whether the DRC link is active or not. If no heartbeat response is received within a certain period, the DRC link can be considered abnormal, and a retry can be initiated. If no heartbeat protocol is received for more than one minute, the device will consider the DRC link idle and exit the DRC link. |

**Example:**

```
{
    "data": {
        "timestamp": 1670415891013
    },
    "method": "heart_beat",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#drc-obstacle-information-report)DRC - Obstacle Information Report

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** hsi_info_push

**Data:**

| Column            | Name                                     | Type  | constraint                                                   | Description                            |
| ----------------- | ---------------------------------------- | ----- | ------------------------------------------------------------ | -------------------------------------- |
| up_distance       | Upward obstacle distance                 | int   | {"max":"","min":"","step":"","unit_name":"Millimeters / mm"} | Upward obstacle distance               |
| down_distance     | Downward obstacle distance               | int   | {"max":"","min":"","step":"","unit_name":"Millimeters / mm"} | Downward obstacle distance             |
| up_enable         | Upward obstacle sensing switch state     | bool  | {"0":"Disable","1":"Enable"}                                 | Upward obstacle sensing switch state   |
| up_work           | Upward obstacle sensing work state       | bool  | {"0":"Disable","1":"Enable"}                                 | Upward obstacle sensing work state     |
| down_enable       | Downward obstacle sensing switch state   | bool  | {"0":"Disable","1":"Enable"}                                 | Downward obstacle sensing switch state |
| down_work         | Downward obstacle sensing work state     | bool  | {"0":"Disable","1":"Enable"}                                 | Downward obstacle sensing work state   |
| around_distances  | Distances to surrounding obstacles       | array | {"size": -, "item_type": int}                                | Distances to surrounding obstacles     |
| left_enable       | Left obstacle sensing switch state       | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| left_work         | Left obstacle sensing working state      | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| right_enable      | Right obstacle sensing switch state      | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| right_work        | Right obstacle sensing working state     | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| front_enable      | Forward obstacle sensing switch state    | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| front_work        | Forward obstacle sensing working state   | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| back_enable       | Backward obstacle sensing switch state   | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| back_work         | Backward obstacle sensing working state  | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| vertical_enable   | Vertical obstacle sensing switch state   | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| vertical_work     | Vertical obstacle sensing working state  | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| horizontal_enable | Horizontal obstacle sensing switch state | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |
| horizontal_work   | Horizontal obstacle sensing work state   | bool  | {"0":"Disable","1":"Enable"}                                 |                                        |

**Example:**

```
{
    "data": {
        "around_distance": [
            10,
            8,
            9,
            16,
            2
        ],
        "back_enable": true,
        "back_work": true,
        "down_distance": 10,
        "down_enable": true,
        "down_work": true,
        "front_enable": true,
        "front_work": true,
        "horizontal_enable": true,
        "horizontal_work": true,
        "left_enable": true,
        "left_work": true,
        "right_enable": true,
        "right_work": true,
        "up_distance": 10,
        "up_enable": true,
        "up_work": true,
        "vertical_enable": true,
        "vertical_work": true
    },
    "timestamp": 1670415891013
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#drc-image-transmission-link-delay-information-reported)DRC - Image transmission link delay information reported

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** delay_info_push

**Data:**

| Column               | Name                                            | Type  | constraint                        | Description                                             |
| -------------------- | ----------------------------------------------- | ----- | --------------------------------- | ------------------------------------------------------- |
| sdr_cmd_delay        | Delay in the transmission protocol command link | int   | {"unit_name":"Milliseconds / ms"} | Delay in the transmission protocol command link         |
| liveview_delay_list  | Video transmission video stream delay           | array | {"size": -, "item_type": struct}  | Video transmission video stream delay, multiple streams |
| »video_id            | Stream number                                   | text  |                                   | Stream number                                           |
| »liveview_delay_time | Stream delay                                    | int   | {"unit_name":"Milliseconds / ms"} | Stream delay                                            |

**Example:**

```
{
    "data": {
        "liveview_delay_list": [
            {
                "liveview_delay_time": 60,
                "video_id": "1581BN210004555439234/52-0-0/normal-0"
            },
            {
                "liveview_delay_time": 80,
                "video_id": "1581BN210004555439234/53-0-0/normal-0"
            }
        ],
        "sdr_cmd_delay": 10
    },
    "timestamp": 1670415891013
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/drc.html#drc-high-frequency-osd-information-pushing)DRC-high frequency osd information pushing

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** osd_info_push

**Data:**

| Column        | Name                                  | Type   | constraint        | Description |
| ------------- | ------------------------------------- | ------ | ----------------- | ----------- |
| attitude_head | Head angle of flight attitude         | double | {"unit":"degree"} |             |
| latitude      | Flight latitude                       | double | {"unit":"degree"} |             |
| longitude     | Flight longitude                      | double | {"unit":"degree"} |             |
| height        | Flight altitude                       | double | {"unit":"degree"} |             |
| speed_x       | Drone speed of X coordinate direction | double | {"unit":"m/s"}    |             |
| speed_y       | Drone speed of Y coordinate direction | double | {"unit":"m/s"}    |             |
| speed_z       | Drone speed of Z coordinate direction | double | {"unit":"m/s"}    |             |
| gimbal_pitch  | Pitch angle of gimbal                 | double | {"unit":"degree"} |             |
| gimbal_roll   | Roll angle of pitch                   | double | {"unit":"degree"} |             |
| gimbal_yaw    | Yaw angle of gimbal                   | double | {"unit":"degree"} |             |

**Example:**

```
{
    "method": "osd_info_push",
    "timestamp": 1670415891013,
    "data": {
        "attitude_head": 60,
        "latitude": 10,
        "longitude": 10,
        "height": 10,
        "speed_x": 10,
        "speed_y": 10,
        "speed_z": 10,
        "gimbal_pitch": 60,
        "gimbal_roll": 60,
        "gimbal_yaw": 60
    }
}
```
