# Live Flight Controls

2026-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/00.mqtt/40.dji-rc-plus-2/30.drc.md) 

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#event)Event

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#authorization-result-notification)Authorization result notification

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** cloud_control_auth_notify

**Data:**

| Column  | Name        | Type        | Constraint                                                                                                                                | Description                  |
| ------- | ----------- | ----------- | ----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| result  | Return code | int         | {"max":"","min":"","step":"","unit_name":null}                                                                                            | Non-zero represents an error |
| output  | Output      | struct      |                                                                                                                                           |                              |
| »status | Task state  | enum_string | {"canceled":"Authorization request popup was canceled due to another user's request.","failed":"Error or user denied","ok":"User agreed"} |                              |

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

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#flyto-execution-result-event-notification)Flyto execution result event notification

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** fly_to_point_progress

**Data:**

| Column              | Name                                 | Type        | constraint                                                                                                                                                                        | Description                                                                                                                            |
| ------------------- | ------------------------------------ | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| fly_to_id           | Flyto target point ID                | text        | {"length":""}                                                                                                                                                                     |                                                                                                                                        |
| status              | State                                | enum_string | {"wayline_cancel":"Cancel flying to target point","wayline_failed":"Execution failed","wayline_ok":"Executed successfully, flown to target point","wayline_progress":"Executing"} |                                                                                                                                        |
| result              | Return code                          | int         | {"max":"","min":"","step":"","unit_name":null}                                                                                                                                    | Non-zero represents an error                                                                                                           |
| way_point_index     | Currently executing the nth waypoint | int         | {"max":"","min":"","step":"","unit_name":null}                                                                                                                                    |                                                                                                                                        |
| remaining_distance  | Remaining mission distance           | float       | {"max":"","min":"","step":0.1,"unit_name":"Meters / m"}                                                                                                                           |                                                                                                                                        |
| remaining_time      | Remaining mission time               | float       | {"max":"","min":"","step":0.1,"unit_name":"seconds / s"}                                                                                                                          |                                                                                                                                        |
| planned_path_points | List of planned trajectory points    | array       | {"size": -, "item_type": struct}                                                                                                                                                  |                                                                                                                                        |
| »latitude           | Latitude of the trajectory point     | double      | {"max":90,"min":-90,"step":"","unit_name":null}                                                                                                                                   | Latitude of the trajectory point, angle value, negative for south latitude, positive for north latitude, accurate to 6 decimal places. |
| »longitude          | Longitude of the trajectory point    | double      | {"max":180,"min":-180,"step":"","unit_name":null}                                                                                                                                 | Longitude of the trajectory point, angle value, east longitude is positive, west longitude is negative, precision to 6 decimal places. |
| »height             | Trajectory point height              | float       | {"step":0.1,"unit_name":"Meters / m"}                                                                                                                                             | Trajectory point height, ellipsoid height                                                                                              |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "fly_to_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
        "planned_path_points": [
            {
                "height": 123.234,
                "latitude": 13.23,
                "longitude": 123.234
            }
        ],
        "remaining_distance": 0,
        "remaining_time": 0,
        "result": 0,
        "status": "wayline_progress",
        "way_point_index": 0
    },
    "need_reply": 1,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 16540709686556,
    "method": "fly_to_point_progress"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#one-key-takeoff-result-event-notification)One-key takeoff result event notification

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** takeoff_to_point_progress

**Data:**

| Column              | Name                                 | Type        | constraint                                                                                                                                                                                                                                                           | Description                                                                                                                            |
| ------------------- | ------------------------------------ | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| status              | Task state                           | enum_string | {"task_finish":"One-key takeoff mission completed","task_ready":"Ready for takeoff","wayline_cancel":"Cancel flying to target point","wayline_failed":"Execution failed","wayline_ok":"Executed successfully, flown to target point","wayline_progress":"Executing"} |                                                                                                                                        |
| result              | Return code                          | int         | {"max":"","min":"","step":"","unit_name":null}                                                                                                                                                                                                                       |                                                                                                                                        |
| flight_id           | One-key takeoff mission UUID         | text        | {"length":""}                                                                                                                                                                                                                                                        |                                                                                                                                        |
| track_id            | Track ID                             | text        | {"length":""}                                                                                                                                                                                                                                                        |                                                                                                                                        |
| way_point_index     | Currently executing the nth waypoint | int         | {"max":"","min":"","step":"","unit_name":null}                                                                                                                                                                                                                       |                                                                                                                                        |
| remaining_distance  | Remaining mission distance           | float       | {"max":"","min":"","step":0.1,"unit_name":"Meters / m"}                                                                                                                                                                                                              |                                                                                                                                        |
| remaining_time      | Remaining mission time               | float       | {"max":"","min":"","step":0.1,"unit_name":"seconds / s"}                                                                                                                                                                                                             |                                                                                                                                        |
| planned_path_points | List of planned trajectory points    | array       | {"size": -, "item_type": struct}                                                                                                                                                                                                                                     |                                                                                                                                        |
| »latitude           | Latitude of the trajectory point     | double      | {"max":90,"min":-90,"step":"","unit_name":null}                                                                                                                                                                                                                      | Latitude of the trajectory point, angle value, negative for south latitude, positive for north latitude, accurate to 6 decimal places. |
| »longitude          | Longitude of the trajectory point    | double      | {"max":180,"min":-180,"step":"","unit_name":null}                                                                                                                                                                                                                    | Longitude of the trajectory point, angle value, east longitude is positive, west longitude is negative, precision to 6 decimal places. |
| »height             | Trajectory point height              | float       | {"max":"","min":"","step":0.1,"unit_name":"Meters / m"}                                                                                                                                                                                                              | Trajectory point height, ellipsoid height                                                                                              |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "flight_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
        "planned_path_points": [
            {
                "height": 123.234,
                "latitude": 13.23,
                "longitude": 123.234
            }
        ],
        "remaining_distance": 0,
        "remaining_time": 0,
        "result": 0,
        "status": "wayline_ok",
        "track_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
        "way_point_index": 1
    },
    "need_reply": 1,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 16540709686556,
    "method": "takeoff_to_point_progress"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#drc-link-state-notification-abandoned)DRC link state notification (Abandoned)

**Note: This protocol is no longer maintained and may be inaccurate. For more accurate DRC link status, use the device attribute “drc_state” reported by the remote controller or the “DRC-heart beat”.**

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** drc_status_notify

**Data:**

| Column    | Name        | Type     | Constraint                                             | Description                  |
| --------- | ----------- | -------- | ------------------------------------------------------ | ---------------------------- |
| result    | Return code | int      |                                                        | Non-zero represents an error |
| drc_state | DRC status  | enum_int | {"0":"Not connected","1":"Connecting","2":"Connected"} |                              |

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
    "timestamp": 1654070968655,
    "method": "drc_status_notify"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#notification-of-drc-flight-control-invalidity-reasons)Notification of DRC - flight control invalidity reasons

The DRC - flight control is an integrated control feature for the aircraft. If it is unavailable, the drone_control capability cannot be used for manual operation.

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** joystick_invalid_notify

**Data:**

| Column | Name       | Type | Constraint                                                                                                                                                                                                                                     | Description |
| ------ | ---------- | ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| reason | Task state | int  | {"0":"Remote controller lost connection","1":"Low battery return","2":"Low battery landing","3":"Close to the flight restriction zone","4":"Remote controller takeover control authority (for example, triggered return, B control takeover)"} |             |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "reason": 0
    },
    "need_reply": 1,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "joystick_invalid_notify"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#flight-control%E2%80%94enter-poi-mode)Flight Control—Enter POI Mode

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** poi_mode_enter

**Data:**

| Column    | Name                        | Type   | Constraint                                                | Description                                                                                              |
| --------- | --------------------------- | ------ | --------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| latitude  | Destination point latitude  | double | {"max":90,"min": -90}                                     | Angle value. Negative for south latitude, and positive for north latitude. Accurate to 6 decimal places. |
| longitude | Destination point longitude | double | {"max":180,"min": -180}                                   | Angle value. Positive for east longitude, negative for west longitude. Accurate to 6 decimal places.     |
| height    | Destination point altitude  | float  | {"max":10000,"min":2,"step":0.1,"unit_name":"Meters / m"} | Destination point altitude (ellipsoidal height), relative to the takeoff point                           |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
    "height": 100,
    "latitude": 12.23,
    "longitude": 12.23
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "poi_mode_enter"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** poi_mode_enter

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
    "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "poi_mode_enter"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#flight-control%E2%80%94exit-poi-mode)Flight Control—Exit POI Mode

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** poi_mode_exit

**Data:** null

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {},
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "poi_mode_exit"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** poi_mode_exit

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
    "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "poi_mode_exit"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#flight-control%E2%80%94poi-circle-speed-setting)Flight Control—POI Circle Speed Setting

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** poi_circle_speed_set

**Data:**

| Column       | Name               | Type  | Constraint                                                                                                      | Description                                                                                               |
| ------------ | ------------------ | ----- | --------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| circle_speed | Circle speed value | float | {"max":"Based on the maximum circle speed reported by poi_status_notify","unit_name":"Meters per second / m/s"} | Circle speed value, negative represents clockwise rotation, positive represents counterclockwise rotation |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
    "circle_speed": 5.2
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "poi_circle_speed_set"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** poi_circle_speed_set

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | return code | int  |            | Non-zero represents an error |

**Example:**

```
{
"bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
"data": {
"result": 0
},
"tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
"timestamp": 1654070968655,
"method": "poi_circle_speed_set"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#flight-control%E2%80%94poi-circle-status-information-notification)Flight Control—POI Circle Status Information Notification

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** poi_status_notify

**Data:**

| Column           | Name             | Type        | Constraint                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | Description |
| ---------------- | ---------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| status           | Status           | enum_string | {"failed":"Failed","in_progress":"In Progress","ok":"Success"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |             |
| reason           | Return code      | int         | {"0":"Normal","1":"Too close to subject","2":"Too far from subject","3":"Reached min altitude","4":"Reached max altitude","5":"Reached max distance","6":"Approaching GEO Zone","7":"Gimbal movement reached limit","8":"Aircraft yaw reached limit for POI (±80°)","9":"Obstacle too close","10":"Positioning source switched (from GPS to RTK), POI Paused","11":"Positioning source switched (from RTK to GPS)","12":"Obstacle sensing unavailable","160":"Normal exit","161":"Payload not compatible","162":"Camera mode not supported (Panorama, High-Res Grid, AI Spot-Check, etc.)","163":"Illegal command","164":"Positioning unavailable","165":"Aircraft not taken off","166":"Flight mode error. Only N mode available","167":"Not available in this mode (RTH, landing, attitude mode)","168":"Lost remote control or video transmission signal"} |             |
| circle_radius    | Circle Radius    | float       | {"unit_name":"Meters / m"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |             |
| circle_speed     | Circle Speed     | float       | {"unit_name":"Meters per second / m/s"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |             |
| max_circle_speed | Max Circle Speed | float       | {"unit_name":"Meters per second / m/s"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |             |

**Example:**

```
{
"bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
"data": {
"circle_radius": 10.1,
"circle_speed": 100.1,
"max_circle_speed": 200.2,
"reason": 0,
"status": "in_progress"
},
"need_reply": 1,
"tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
"timestamp": 16540709686556,
"method": "poi_status_notify"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#upload-capture-progress)Upload capture progress

When the photo-taking action needs to continue, progress will be reported through this event. Currently, only the panoramic photo mode is supported.

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** camera_photo_take_progress

**Data:**

| Column         | Name                | Type        | Constraint                                                                                                            | Description                  |
| -------------- | ------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| output         | Output              | struct      |                                                                                                                       |                              |
| »status        | Capturing state     | enum_string | {"fail":"Failed","in_progress":"In progress","ok":"Finished"}                                                         |                              |
| »progress      | Progress            | struct      |                                                                                                                       |                              |
| »»current_step | Executing result    | enum_int    | {"3000":"Capturing panorama has not started or is finished","3002":"Capturing panorama","3005":"Generating panorama"} |                              |
| »»percent      | Percent             | int         | {"max":"100","min":"0","step":"1"}                                                                                    |                              |
| »ext           | Expanded content    | struct      |                                                                                                                       |                              |
| »»camera_mode  | Current camera mode | enum_int    | {"3":"Panorama"}                                                                                                      |                              |
| result         | Return code         | int         | {"max":"","min":"","step":"","unit_name":null}                                                                        | Non-zero represents an error |

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
    "timestamp": 1654070968655,
    "method": "camera_photo_take_progress"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#drc-delay-information-pushing-of-image-transmission-link)DRC-delay information pushing of image transmission link

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** delay_info_push

**Data:**

| Column               | Name                                            | Type  | Constraint                                                    | Description                                     |
| -------------------- | ----------------------------------------------- | ----- | ------------------------------------------------------------- | ----------------------------------------------- |
| sdr_cmd_delay        | Delay in the transmission protocol command link | int   | {"max":"","min":"","step":"","unit_name":"milliseconds / ms"} | Delay in the transmission protocol command link |
| liveview_delay_list  | Video transmission video stream delay           | array | {"size": -, "item_type": struct}                              |                                                 |
| »video_id            | Stream number                                   | text  | {"length":""}                                                 | Stream number                                   |
| »liveview_delay_time | Stream delay                                    | int   | {"max":"","min":"","step":"","unit_name":"milliseconds / ms"} | Stream delay                                    |

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
    "timestamp": 1670415891013,
    "method": "delay_info_push"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#drc-obstacle-avoidance-information-pushing)DRC-obstacle avoidance information pushing

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** hsi_info_push

**Data:**

| Column            | Name                                     | Type  | constraint                                                   | Description |
| ----------------- | ---------------------------------------- | ----- | ------------------------------------------------------------ | ----------- |
| up_distance       | Upward obstacle distance                 | int   | {"max":"","min":"","step":"","unit_name":"Millimeters / mm"} |             |
| down_distance     | Downward obstacle distance               | int   | {"max":"","min":"","step":"","unit_name":"Millimeters / mm"} |             |
| up_enable         | Upward obstacle sensing switch state     | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| up_work           | Upward obstacle sensing work state       | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| down_enable       | Downward obstacle sensing switch state   | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| down_work         | Downward obstacle sensing work state     | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| around_distances  | Distances to surrounding obstacles       | array | {"size": -, "item_type": int}                                |             |
| left_enable       | Left obstacle sensing switch state       | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| left_work         | Left obstacle sensing working state      | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| right_enable      | Right obstacle sensing switch state      | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| right_work        | Right obstacle sensing working state     | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| front_enable      | Forward obstacle sensing switch state    | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| front_work        | Forward obstacle sensing working state   | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| back_enable       | Backward obstacle sensing switch state   | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| back_work         | Backward obstacle sensing working state  | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| vertical_enable   | Vertical obstacle sensing switch state   | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| vertical_work     | Vertical obstacle sensing working state  | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| horizontal_enable | Horizontal obstacle sensing switch state | bool  | {"0":"Disable","1":"Enable"}                                 |             |
| horizontal_work   | Horizontal obstacle sensing work state   | bool  | {"0":"Disable","1":"Enable"}                                 |             |

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
    "timestamp": 1670415891013,
    "method": "hsi_info_push"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#return-home-information)Return home information

**Topic:** thing/product/*{gateway_sn}*/events

**Direction:** up

**Method:** return_home_info

**Data:**

| Column              | Name                                            | Type   | Constraint                                              | Description                                                                                                                            |
| ------------------- | ----------------------------------------------- | ------ | ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| planned_path_points | List of planned trajectory points               | array  | {"size": -, "item_type": struct}                        |                                                                                                                                        |
| »latitude           | Latitude of the trajectory point (angle value)  | double | {"max":90,"min":-90}                                    | Latitude of the trajectory point, angle value, negative for south latitude, positive for north latitude, accurate to 6 decimal places. |
| »longitude          | Longitude of the trajectory point (angle value) | double | {"max":180,"min":-180}                                  | Longitude of the trajectory point, angle value, east longitude is positive, west longitude is negative, precision to 6 decimal places. |
| »height             | Trajectory point height                         | float  | {"max":"","min":"","step":0.1,"unit_name":"Meters / m"} | Trajectory point height, ellipsoid height                                                                                              |
| flight_id           | Mission ID                                      | text   | {"length":""}                                           |                                                                                                                                        |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "flight_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
        "planned_path_points": [
            {
                "height": 107.91999816894531,
                "latitude": 22.591622374020517,
                "longitude": 114.00506408885121
            },
            {
                "height": 107.91999816894531,
                "latitude": 22.591547439806163,
                "longitude": 114.00506366975605
            }
        ]
    },
    "gateway": "7XJDLC10010639",
    "method": "return_home_info",
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1734446832046
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#service)Service

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#request-cloud-control-authorization)Request cloud control authorization

After the request, a "Request Authorization" pop-up will appear on the remote controller.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** cloud_control_auth_request

**Data:**

| Column        | Name          | Type  | Constraint                       | Description                                                       |
| ------------- | ------------- | ----- | -------------------------------- | ----------------------------------------------------------------- |
| user_id       | User ID       | text  | {"length":""}                    | The user ID of the cloud user requesting authorization.           |
| user_callsign | User Callsign | text  | {"length":""}                    | The nickname of the cloud user requesting authorization.          |
| control_keys  | Control Keys  | array | { "size": 1, "item_type": text } | To request flight control authorization, please fill in "flight". |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "control_keys": [
            "flight"
        ],
        "user_callsign": "xxxxxxx",
        "user_id": "xxxxxxxxxxx"
    },
    "method": "cloud_control_auth_request",
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1704038400000
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** cloud_control_auth_request

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

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

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#release-cloud-control)Release cloud control

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** cloud_control_release

**Data:**

| Column       | Name             | Type  | Constraint                       | Description                                                                       |
| ------------ | ---------------- | ----- | -------------------------------- | --------------------------------------------------------------------------------- |
| control_keys | Control key list | array | { "size": 1, "item_type": text } | List of control rights to be released. "Flight" represents flight control rights. |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "control_keys": [
            "flight"
        ]
    },
    "method": "cloud_control_release",
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1704038400000
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** cloud_control_release

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

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

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#enter-live-flight-controls-mode)Enter live flight controls mode

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** drc_mode_enter

**Data:**

| Column        | Name                                       | Type   | constraint                                  | Description                                                                                                                                                                           |
| ------------- | ------------------------------------------ | ------ | ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| mqtt_broker   | Broker connection information              | struct |                                             | Get the address and authentication information of the MQTT relay service                                                                                                              |
| »address      | Server connection address                  | text   |                                             | Server connection address, for example, 192.0.2.1:8883, mqtt.dji.com:8883                                                                                                             |
| »client_id    | Client ID                                  | text   |                                             | Customizable MQTT client ID.It is recommended to use the device's SN code, or combine it with a semantic prefix, e.g., drc-4J4R101                                                    |
| »username     | User name                                  | text   |                                             | Username used when establishing a connection                                                                                                                                          |
| »password     | Password                                   | text   |                                             | Password required for authentication when establishing a connection                                                                                                                   |
| »expire_time  | Authentication information expiration time | int    | {"unit_name":"Seconds / s"}                 | Authentication information can be reused within the validity period. The expiration of authentication information does not affect devices that have already established a connection. |
| »enable_tls   | Whether to enable TLS                      | bool   |                                             | Enable TLS, i.e., encrypt the MQTT link                                                                                                                                               |
| osd_frequency | OSD frequency                              | int    | {"max":30,"min":1,"unit_name":"Hertz / Hz"} | Set OSD reporting frequency                                                                                                                                                           |
| hsi_frequency | HSI frequency                              | int    | {"max":30,"min":1,"unit_name":"Hertz / Hz"} | Set HSI reporting frequency                                                                                                                                                           |

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
    "timestamp": 1654070968655,
    "method": "drc_mode_enter"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** drc_mode_enter

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
    "method": "drc_mode_enter"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#exit-live-flight-controls-mode)Exit live flight controls mode

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

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#one-key-takeoff)One-key takeoff

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** takeoff_to_point

**Data:**

| Column                      | Name                                            | Type     | Constraint                                                                                                                           | Description                                                                                                                                                                                                                                                                                                                                 |
| --------------------------- | ----------------------------------------------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| target_latitude             | Target point latitude (in degree values)        | double   | {"max":90,"min":-90,"step":"","unit_name":null}                                                                                      |                                                                                                                                                                                                                                                                                                                                             |
| target_longitude            | Target point longitude (in degree values)       | double   | {"max":180,"min":-180,"step":"","unit_name":null}                                                                                    |                                                                                                                                                                                                                                                                                                                                             |
| target_height               | Target point height                             | float    | {"max":1500,"min":2,"step":0.1,"unit_name":"Meters / m"}                                                                             |                                                                                                                                                                                                                                                                                                                                             |
| security_takeoff_height     | Safe takeoff height                             | float    | {"max":1500,"min":20,"step":0.1,"unit_name":"Meters / m"}                                                                            |                                                                                                                                                                                                                                                                                                                                             |
| rth_mode                    | [Required] Return home mode setting value       | enum_int | {"0":"Intelligent altitude","1":"Preset altitude"}                                                                                   |                                                                                                                                                                                                                                                                                                                                             |
| rth_altitude                | Return home altitude                            | int      | {"max":1500,"min":2,"step":1,"unit_name":"Meters / m"}                                                                               |                                                                                                                                                                                                                                                                                                                                             |
| rc_lost_action              | Remote controller lost control action           | enum_int | {"0":"Hovering","1":"Landing","2":"Returning to home"}                                                                               |                                                                                                                                                                                                                                                                                                                                             |
| commander_mode_lost_action  | [Required] FlyTo task signal lost action        | enum_int | {"0":"Continue with the to-point flight mission","1":"Exit the to-point flight mission and perform normal loss of control behavior"} |                                                                                                                                                                                                                                                                                                                                             |
| commander_flight_mode       | [Required] FlyTo task mode value                | enum_int | {"0":"Optimal height flight","1":"Preset height flight"}                                                                             |                                                                                                                                                                                                                                                                                                                                             |
| commander_flight_height     | [Required] FlyTo height                         | float    | {"max":3000,"min":2,"step":0.1,"unit_name":"Meters / m"}                                                                             |                                                                                                                                                                                                                                                                                                                                             |
| flight_id                   | One-key takeoff mission UUID                    | text     | {"length":""}                                                                                                                        |                                                                                                                                                                                                                                                                                                                                             |
| max_speed                   | Achievable maximum speed during one-key takeoff | int      | {"max":15,"min":1,"step":"","unit_name":"meters per second / m/s"}                                                                   |                                                                                                                                                                                                                                                                                                                                             |
| flight_safety_advance_check | Pre-check for flight safety                     | bool     | {"0":"Disable","1":"Enable"}                                                                                                         | Set to pre-check the flight safety for Take Off and flight route task. This field is optional. The default value is 0, which represents closed. 1 represents open. Checking if the local operating area file is consistent with the cloud side before the task starts. If the files are different, pull file from the cloud side to update. |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "commander_flight_height": 80,
        "commander_mode_lost_action": 1,
        "flight_id": "ABDEAC21DCADDA",
        "flight_safety_advance_check": 1,
        "max_speed": 12,
        "rc_lost_action": 0,
        "rth_altitude": 100,
        "security_takeoff_height": 100,
        "target_height": 100,
        "target_latitude": 12.23,
        "target_longitude": 12.32
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "takeoff_to_point"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** takeoff_to_point

**Data:**

| Column | Name        | Type | Constraint                                     | Description |
| ------ | ----------- | ---- | ---------------------------------------------- | ----------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} |             |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "takeoff_to_point"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#fly-to-target-point)Fly to target point

Note: To ensure the flight safety, the min flight altitude of the aircraft is 20 m. If the altitude relative to the takeoff point is below 20 m, the aircraft will first ascend to 20 m.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** fly_to_point

**Data:**

| Column     | Name                                      | Type   | Constraint                                                | Description                                                                                                       |
| ---------- | ----------------------------------------- | ------ | --------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| fly_to_id  | Flyto target point ID                     | text   |                                                           |                                                                                                                   |
| max_speed  | Achievable maximum speed during flyto     | int    | {"max":15,"min":0,"unit_name":"Meters per second / m/s"}  |                                                                                                                   |
| points     | List of flyto target points               | array  | {"size": -, "item_type": struct}                          | Only supports 1 target point                                                                                      |
| »latitude  | Target point latitude (in degree values)  | double | {"max":90,"min":-90}                                      | Target point latitude, in degree values. Negative for south, positive for north. Precision up to 6 decimal places |
| »longitude | Target point longitude (in degree values) | double | {"max":180,"min":-180}                                    | Target point longitude, in degree values. Positive for east, negative for west. Precision up to 6 decimal places  |
| »height    | Target point height                       | float  | {"max":10000,"min":2,"step":0.1,"unit_name":"Meters / m"} | Target point height (ellipsoid height), using the WGS84 model                                                     |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "fly_to_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
        "max_speed": 12,
        "points": [
            {
                "height": 100,
                "latitude": 12.23,
                "longitude": 12.23
            }
        ]
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "fly_to_point"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** fly_to_point

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
    "method": "fly_to_point"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#end-flyto-task-to-target-point)End Flyto task to target point

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** fly_to_point_stop

**Data:** null

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {},
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "fly_to_point_stop"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** fly_to_point_stop

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
    "method": "fly_to_point_stop"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#update-the-target-point-of-flyto)Update the target point of flyto

During the process of one-key takeoff or flyto target point, the target point can be quickly updated through this command.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** fly_to_point_update

**Data:**

| Column     | Name                                                     | Type   | Constraint                                                             | Description |
| ---------- | -------------------------------------------------------- | ------ | ---------------------------------------------------------------------- | ----------- |
| max_speed  | Achievable maximum speed during the live flight controls | int    | {"max":"5","min":"-4","step":"","unit_name":"meters per second / m/s"} |             |
| points     | Updated target points list                               | array  | {"size": -, "item_type": struct}                                       |             |
| »latitude  | Longitude                                                | double | {"max":"","min":"","step":"","unit_name":null}                         |             |
| »longitude | Longitude                                                | double | {"max":"","min":"","step":"","unit_name":null}                         |             |
| »height    | Height                                                   | float  | {"max":"","min":"","step":"","unit_name":null}                         |             |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "max_speed": 12,
        "points": [
            {
                "height": 100,
                "latitude": 12.23,
                "longitude": 12.23
            }
        ]
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "fly_to_point_update"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** fly_to_point_update

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "fly_to_point_update"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#one-key-return)One-key return

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** return_home

**Data:** null

**Example:**

```
{
    "bid": "f9f07aad-d1f1-4dc1-8ad0-a3417fd365cc",
    "data": null,
    "method": "return_home",
    "tid": "b103b00a-3fcc-476e-9cb6-bc5e27d2defd",
    "timestamp": 1734425015702
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** return_home

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "bid": "f9f07aad-d1f1-4dc1-8ad0-a3417fd365cc",
    "data": {
        "result": 0
    },
    "method": "return_home",
    "tid": "b103b00a-3fcc-476e-9cb6-bc5e27d2defd",
    "timestamp": 1734425014635
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#cancel-return)Cancel return

After returning home, the aircraft will exit the wayline mode. If return is canceled at this point, the aircraft will hover.

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** return_home_cancel

**Data:** null

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {},
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "return_home_cancel"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** return_home_cancel

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "result": 0
    },
    "need_reply": 1,
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp": 1654070968655,
    "method": "return_home_cancel"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/drc.html#drc-high-frequency-osd-information-pushing)DRC-high frequency osd information pushing

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
