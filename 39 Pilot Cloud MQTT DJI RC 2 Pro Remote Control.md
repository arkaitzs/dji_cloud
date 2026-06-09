# Remote Control

2025-05-21

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/00.mqtt/40.dji-rc-plus-2/40.remote-control.md) 

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#event)Event

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#camera-osd-push)Camera OSD Push

Used for pushing camera related information. The "seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_osd_info_push

**Data:**

| Column                                | Name                                                    | Type     | Constraint                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Description                                                                                                                                                                                                         |
| ------------------------------------- | ------------------------------------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index                         | Camera enumeration                                      | text     | {"length":""}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| wide_lense                            | Wide-angle lens info                                    | struct   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| »wide_exposure_mode                   | Wide-angle lens exposure mode                           | enum_int | {"1":"Auto","2":"Shutter priority","3":"Aperture priority","4":"Manual exposure"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Current configuration of the wide-angle lens exposure mode                                                                                                                                                          |
| »wide_iso                             | Wide-angle lens ISO                                     | enum_int | {"0":"Auto","1":"Auto (High Sense)","2":"50","3":"100","4":"200","5":"400","6":"800","7":"1600","8":"3200","9":"6400","10":"12800","11":"25600","255":"FIXED"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Current configuration of the wide-angle lens ISO                                                                                                                                                                    |
| » wide_shutter_speed                  | Wide-angle lens shutter speed                           | enum_int | {"0":"1/8000 s","1":"1/6400 s","2":"1/6000 s","3":"1/5000 s","4":"1/4000 s","5":"1/3200 s","6":"1/3000 s","7":"1/2500 s","8":"1/2000 s","9":"1/1600 s","10":"1/1500 s","11":"1/1250 s","12":"1/1000 s","13":"1/800 s","14":"1/725 s","15":"1/640 s","16":"1/500 s","17":"1/400 s","18":"1/350 s","19":"1/320 s","20":"1/250 s","21":"1/240 s","22":"1/200 s","23":"1/180 s","24":"1/160 s","25":"1/125 s","26":"1/120 s","27":"1/100 s","28":"1/90 s","29":"1/80 s","30":"1/60 s","31":"1/50 s","32":"1/40 s","33":"1/30 s","34":"1/25 s","35":"1/20 s","36":"1/15 s","37":"1/12.5 s","38":"1/10 s","39":"1/8 s","40":"1/6.25 s","41":"1/5 s","42":"1/4 s","43":"1/3 s","44":"1/2.5 s","45":"1/2 s","46":"1/1.67 s","47":"1/1.25 s","48":"1.0 s","49":"1.3 s","50":"1.6 s","51":"2.0 s","52":"2.5 s","53":"3.0 s","54":"3.2 s","55":"4.0 s","56":"5.0 s","57":"6.0 s","58":"7.0 s","59":"8.0 s","65534":"Auto"} | Current configuration of the wide-angle lens shutter speed                                                                                                                                                          |
| »wide_exposure_value                  | Wide-angle lens exposure value                          | enum_int | {"1":"-5.0EV","2":"-4.7EV","3":"-4.3EV","4":"-4.0EV","5":"-3.7EV","6":"-3.3EV","7":"-3.0EV","8":"-2.7EV","9":"-2.3EV","10":"-2.0EV","11":"-1.7EV","12":"-1.3EV","13":"-1.0EV","14":"-0.7EV","15":"-0.3EV","16":"0EV","17":"0.3EV","18":"0.7EV","19":"1.0EV","20":"1.3EV","21":"1.7EV","22":"2.0EV","23":"2.3EV","24":"2.7EV","25":"3.0EV","26":"3.3EV","27":"3.7EV","28":"4.0EV","29":"4.3EV","30":"4.7EV","31":"5.0EV","255":"FIXED"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Current configuration of the wide-angle lens exposure value                                                                                                                                                         |
| »wide_aperture_value                  | Aperture value                                          | enum_int | {"0":"F_AUTO","50":"F0.5","70":"F0.7","80":"F0.8","90":"F0.9","95":"F0.95","100":"F1.0","110":"F1.1","120":"F1.2","140":"F1.4","160":"F1.6","170":"F1.7","180":"F1.8","200":"F2","220":"F2.2","240":"F2.4","250":"F2.5","280":"F2.8","320":"F3.2","330":"F3.3","340":"F3.4","350":"F3.5","400":"F4","440":"F4.4","450":"F4.5","480":"F4.8","500":"F5","560":"F5.6","630":"F6.3","670":"F6.7","680":"F6.8","710":"F7.1","800":"F8","900":"F9","950":"F9.5","960":"F9.6","1000":"F10","1100":"F11","1300":"F13","1400":"F14","1600":"F16","1800":"F18","1900":"F19","2000":"F20","2200":"F22","2500":"F25","2700":"F27","2900":"F29","3200":"F32","3600":"F36","3800":"F38","4000":"F40","4500":"F45","5100":"F51","5400":"F54","5700":"F57","6400":"F64","7200":"F72","7600":"F76","8000":"F80","9000":"F90","10700":"F107","12800":"F128","18000":"F180","25600":"F256"}                                        | Depending on the aircraft model, the range may vary.                                                                                                                                                                |
| zoom_lense                            | Zoom lens info                                          | struct   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » zoom_exposure_mode                  | Zoom lens exposure mode                                 | enum_int | {"1":"Auto","2":"Shutter priority","3":"Aperture priority","4":"Manual exposure"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Current configuration of the zoom lens exposure mode                                                                                                                                                                |
| » zoom_iso                            | Zoom lens ISO                                           | enum_int | {"0":"Auto","1":"Auto (High Sense)","2":"50","3":"100","4":"200","5":"400","6":"800","7":"1600","8":"3200","9":"6400","10":"12800","11":"25600","255":"FIXED"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Current configuration of the zoom lens ISO                                                                                                                                                                          |
| »zoom_shutter_speed                   | Zoom lens shutter speed                                 | enum_int | {"0":"1/8000 s","1":"1/6400 s","2":"1/6000 s","3":"1/5000 s","4":"1/4000 s","5":"1/3200 s","6":"1/3000 s","7":"1/2500 s","8":"1/2000 s","9":"1/1600 s","10":"1/1500 s","11":"1/1250 s","12":"1/1000 s","13":"1/800 s","14":"1/725 s","15":"1/640 s","16":"1/500 s","17":"1/400 s","18":"1/350 s","19":"1/320 s","20":"1/250 s","21":"1/240 s","22":"1/200 s","23":"1/180 s","24":"1/160 s","25":"1/125 s","26":"1/120 s","27":"1/100 s","28":"1/90 s","29":"1/80 s","30":"1/60 s","31":"1/50 s","32":"1/40 s","33":"1/30 s","34":"1/25 s","35":"1/20 s","36":"1/15 s","37":"1/12.5 s","38":"1/10 s","39":"1/8 s","40":"1/6.25 s","41":"1/5 s","42":"1/4 s","43":"1/3 s","44":"1/2.5 s","45":"1/2 s","46":"1/1.67 s","47":"1/1.25 s","48":"1.0 s","49":"1.3 s","50":"1.6 s","51":"2.0 s","52":"2.5 s","53":"3.0 s","54":"3.2 s","55":"4.0 s","56":"5.0 s","57":"6.0 s","58":"7.0 s","59":"8.0 s","65534":"Auto"} | Current configuration of the zoom lens shutter speed                                                                                                                                                                |
| »zoom_exposure_value                  | Zoom lens exposure value                                | enum_int | {"1":"-5.0EV","2":"-4.7EV","3":"-4.3EV","4":"-4.0EV","5":"-3.7EV","6":"-3.3EV","7":"-3.0EV","8":"-2.7EV","9":"-2.3EV","10":"-2.0EV","11":"-1.7EV","12":"-1.3EV","13":"-1.0EV","14":"-0.7EV","15":"-0.3EV","16":"0EV","17":"0.3EV","18":"0.7EV","19":"1.0EV","20":"1.3EV","21":"1.7EV","22":"2.0EV","23":"2.3EV","24":"2.7EV","25":"3.0EV","26":"3.3EV","27":"3.7EV","28":"4.0EV","29":"4.3EV","30":"4.7EV","31":"5.0EV","255":"FIXED"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Current configuration of the zoom lens exposure value                                                                                                                                                               |
| »zoom_focus_mode                      | Zoom lens focus mode                                    | enum_int | {"0":"MF","1":"AFS","2":"AFC"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |                                                                                                                                                                                                                     |
| »zoom_focus_value                     | Zoom lens focus value                                   | int      |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » zoom_max_focus_value                | Zoom lens maximum focus value                           | int      |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » zoom_min_focus_value                | Zoom lens minimum focus value                           | int      |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » zoom_calibrate_farthest_focus_value | Zoom lens calibrated farthest focus value               | int      |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Focused value at the farthest clear position                                                                                                                                                                        |
| » zoom_calibrate_nearest_focus_value  | Zoom lens calibrated nearest focus value                | int      |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Focused value at the nearest clear position                                                                                                                                                                         |
| » zoom_focus_state                    | Zoom lens focus state                                   | int      | {"max":"","min":"","step":"","unit_name":null}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |                                                                                                                                                                                                                     |
| » zoom_factor                         | Zoom factor                                             | int      | {"max":"","min":"","step":"","unit_name":null}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |                                                                                                                                                                                                                     |
| » zoom_aperture_value                 | Zoom lens aperture value                                | enum_int | {"440":"F4.4"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Depending on the aircraft model, the range may vary.                                                                                                                                                                |
| measure_target                        | Target ranging and height measurement                   | struct   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » measure_target_longitude            | Laser ranging target longitude                          | double   | {"max":"180","min":"-180","unit":"Degrees / °"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » measure_target_latitude             | Laser ranging target latitude                           | double   | {"max":"90","min":"-90","unit":"Degrees / °"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |                                                                                                                                                                                                                     |
| » measure_target_altitude             | Laser ranging target altitude                           | double   | {"unit":"Meters / m"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |                                                                                                                                                                                                                     |
| » measure_target_distance             | Laser ranging distance                                  | double   | {"unit":"Meters / m"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |                                                                                                                                                                                                                     |
| ir_lense                              | Infrared information                                    | struct   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » screen_split_enable                 | Whether split screen is enabled                         | bool     | {"false":"Disable split screen","true":"Enable split screen"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |                                                                                                                                                                                                                     |
| » ir_zoom_factor                      | Infrared zoom factor                                    | float    | {"max":20,"min":2}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Infrared zoom factor                                                                                                                                                                                                |
| » thermal_supported_palette_styles    | Collection of supported palette styles by the device    | array    | {"size": -, "item_type": enum_int}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | The capability of supported styles varies for different devices                                                                                                                                                     |
| » thermal_gain_mode                   | Gain Mode                                               | enum_int | {"0":"Auto","1":"Low Gain. Temperature Range is from 0°C to 500°C","2":"High Gain. Temperature Range is from -20°C to 150°C"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   | Low gain provides a larger temperature measurement range, while high gain offers higher temperature measurement accuracy.                                                                                           |
| » thermal_isotherm_state              | Whether isotherm is enabled                             | enum_int | {"0":"Disabled","1":"Enabled"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Isotherm allows users to observe the content of temperature ranges of interest, making objects in the temperature range more prominent.                                                                             |
| » thermal_isotherm_upper_limit        | Upper limit of the temperature range for isotherm       | int      | {"unit":"Celsius / °C"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Effective only when Isotherm is enabled                                                                                                                                                                             |
| » thermal_isotherm_lower_limit        | Lower limit of the temperature range for isotherm       | int      | {"unit":"Celsius / °C"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Effective only when Isotherm is enabled                                                                                                                                                                             |
| » thermal_global_temperature_min      | Minimum temperature measured in the overall view        | float    | {"unit":"Celsius / °C"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |                                                                                                                                                                                                                     |
| » thermal_global_temperature_max      | Maximum temperature measured in the overall view        | float    | {"unit":"Celsius / °C"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |                                                                                                                                                                                                                     |
| liveview                              | Liveview area information                               | struct   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |                                                                                                                                                                                                                     |
| » liveview_world_region               | Field of view (FOV) region in liveview                  | struct   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | The field of view angle of the zoom camera relative to the wide-angle camera or infrared camera may differ in liveview. The coordinate origin is the upper-left corner of the lens.                                 |
| »» left                               | X-axis starting point in the top-left corner            | float    |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | X-axis starting point in the top-left corner                                                                                                                                                                        |
| »» top                                | Top-left corner's starting point on the y-axis          | float    |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Top-left corner's starting point on the y-axis                                                                                                                                                                      |
| »» right                              | Starting point of the x-axis in the bottom right corner | float    |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Starting point of the x-axis in the bottom right corner                                                                                                                                                             |
| »» bottom                             | Y-axis starting point in the lower right corner         | float    |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Y-axis starting point in the lower right corner                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "ir_lense": {
            "ir_zoom_factor": 2,
            "screen_split_enable": false,
            "thermal_current_palette_style": 11,
            "thermal_gain_mode": 2,
            "thermal_global_temperature_max": 40.0373764038086,
            "thermal_global_temperature_min": 31.65154457092285,
            "thermal_isotherm_lower_limit": -20,
            "thermal_isotherm_state": 0,
            "thermal_isotherm_upper_limit": 150
        },
        "liveview": {
            "liveview_world_region": {
                "bottom": 0.5609484910964966,
                "left": 0.43238765001297,
                "right": 0.5639060735702515,
                "top": 0.433199942111969
            }
        },
        "measure_target": {
            "measure_target_altitude": 34.60000228881836,
            "measure_target_distance": 0,
            "measure_target_error_state": 1,
            "measure_target_latitude": 22.907619920797877,
            "measure_target_longitude": 113.70345426744846
        },
        "payload_index": "89-0-0",
        "wide_lense": {
            "wide_aperture_value": 10,
            "wide_exposure_mode": 1,
            "wide_exposure_value": 16,
            "wide_iso": 8,
            "wide_shutter_speed": 45
        },
        "zoom_lense": {
            "zoom_aperture_value": 10,
            "zoom_calibrate_farthest_focus_value": 34,
            "zoom_calibrate_nearest_focus_value": 64,
            "zoom_exposure_mode": 1,
            "zoom_exposure_value": 16,
            "zoom_factor": 6.999994214380596,
            "zoom_focus_mode": 0,
            "zoom_focus_state": 0,
            "zoom_focus_value": 34,
            "zoom_iso": 8,
            "zoom_max_focus_value": 64,
            "zoom_min_focus_value": 33,
            "zoom_shutter_speed": 45
        }
    },
    "method": "drc_camera_osd_info_push",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#aircraft-status-info-report)Aircraft Status Info Report

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_drone_state_push

**Data:**

| Column                  | Name                   | Type     | Constraint                                                                                                                                                                                                                                                                                                                                                                                                                          | Description                                                                                                 |
| ----------------------- | ---------------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| stealth_state           | Discreet mode state    | bool     | {"0":"Disabled","1":"Enabled"}                                                                                                                                                                                                                                                                                                                                                                                                      |                                                                                                             |
| night_lights_state      | Beacon state           | bool     | {"0":"Disabled","1":"Enabled"}                                                                                                                                                                                                                                                                                                                                                                                                      |                                                                                                             |
| mode_code               | Aircraft state         | enum_int | {"0":"Standby","1":"Takeoff preparation","2":"Takeoff preparation completed","3":"Manual flight","4":"Automatic takeoff","5":"Wayline flight","6":"Panorama","7":"Intelligent tracking","8":"ADS-B avoidance","9":"Automatic returning to home","10":"Automatic landing","11":"Forced landing","12":"Three-blade landing","13":"Upgrading","14":"Not connected","15":"APAS","16":"Virtual stick state","17":"Live flight Controls"} |                                                                                                             |
| landing_type            | Landing type           | enum_int | {"0":"Not landed","1":"Landing within dock","2":"Landing at alternate site","3":"Landing triggered by users","4":"Landing triggered by aircraft"}                                                                                                                                                                                                                                                                                   |                                                                                                             |
| landing_protection_type | Landing detection type | enum_int | {"0":"Detection is not opened","1":"Detecting uneven ground or water surface below, which is not suitable for landing. Landing task has exited","2":"No ground detecting result. Landing task has exited","3":"Detection for landing within dock"}                                                                                                                                                                                  | When obstacle sensing is enabled, the aircraft will activate landing detection during an emergency landing. |

**Example:**

```
{
    "data": {
        "landing_protection_type": 0,
        "landing_type": 1,
        "mode_code": 0,
        "night_lights_state": 0,
        "stealth_state": 0
    },
    "method": "drc_drone_state_push",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#camera-state-report)Camera State Report

Report camera information when it changes. ("seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".)

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_state_push

**Data:**

| Column                   | Name                                 | Type        | Constraint                                                                               | Description                                                                                                                                                                                                         |
| ------------------------ | ------------------------------------ | ----------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index            | Camera enumeration                   | text        | {"length":""}                                                                            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_state             | Camera status information            | struct      |                                                                                          |                                                                                                                                                                                                                     |
| »camera_mode             | Camera status                        | enum_int    | {"0":"Capturing","1":"Recording","2":"Smart Low-Light","3":"Panorama","4":"Timed Shot"}  |                                                                                                                                                                                                                     |
| »interval_photo_interval | Time interval of Timed Shot          | int         | {"max":"","min":"","step":"","unit_name":"Second / s"}                                   |                                                                                                                                                                                                                     |
| »video_resolution        | Video resolution                     | enum_string | {"0":"1920×1080","1":"3840×2160"}                                                        |                                                                                                                                                                                                                     |
| »linkage_zoom_state      | Link Zoom status                     | bool        | {"0":"Disabled","1":"Enabled"}                                                           |                                                                                                                                                                                                                     |
| »photo_size              | Photo size                           | enum_int    | {"0":"Default","1":"Extra small","2":"Small","3":"Medium","4":"Large","5":"Extra large"} |                                                                                                                                                                                                                     |
| »record_time             | Video recording duration             | int         | {"max":"","min":"","step":"","unit_name":"Second / s"}                                   |                                                                                                                                                                                                                     |
| »recording_state         | Recording status                     | enum_int    | {"0":"Idle","1":"Recording"}                                                             |                                                                                                                                                                                                                     |
| »photo_state             | Capturing state                      | enum_int    | {"0":"Idle","1":"Capturing"}                                                             |                                                                                                                                                                                                                     |
| »remain_photo_num        | Remaining number of photos           | int         | {"max":"","min":"","step":"","unit_name":null}                                           |                                                                                                                                                                                                                     |
| »remain_record_duration  | Remaining time of recording          | int         | {"max":"","min":"","step":"","unit_name":"Second / s"}                                   |                                                                                                                                                                                                                     |
| media_storage            | Media storage information            | struct      |                                                                                          |                                                                                                                                                                                                                     |
| »photo_storage_settings  | Photo storage settings collection    | array       | {"size": -, "item_type": text}                                                           | Capturing storage types are {current, vision, ir}. Multiple selections available based on the models.                                                                                                               |
| »video_storage_settings  | Collection of video storage settings | array       | {"size": -, "item_type": text}                                                           | Video storage types are {current, vision, ir}. Multiple selections available based on the models.                                                                                                                   |

**Example:**

```
{
    "data": {
        "camera_state": {
            "camera_mode": 0,
            "interval_photo_interval": 2.5,
            "linkage_zoom_state": 0,
            "photo_size": 1,
            "photo_state": 0,
            "record_time": 0,
            "recording_state": 0,
            "remain_photo_num": 6727,
            "remain_record_duration": 0,
            "video_resolution": 0
        },
        "media_storage": {
            "photo_storage_settings": [
                "current",
                "ir"
            ],
            "video_storage_settings": [
                "current",
                "ir"
            ]
        },
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_state_push",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#report-capturing-information)Report Capturing Information

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_photo_info_push

**Data:**

| Column         | Name                     | Type        | Constraint                                                                                                            | Description                  |
| -------------- | ------------------------ | ----------- | --------------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| countdown_time | Countdown for Timed Shot | int         | {"max":"","min":"","step":"","unit_name":null}                                                                        |                              |
| result         | Capturing result         | int         | {"max":"","min":"","step":"","unit_name":null}                                                                        | Non-zero represents an error |
| status         | State                    | enum_string | {"fail":"Failed","in_progress":"In progress","ok":"Finished"}                                                         |                              |
| progress       | Progress                 | struct      |                                                                                                                       |                              |
| »current_step  | Execution step           | enum_int    | {"3000":"Capturing panorama has not started or is finished","3002":"Capturing panorama","3005":"Generating panorama"} |                              |
| »percent       | Percent                  | int         | {"max":"100","min":"0","step":"","unit_name"}                                                                         |                              |
| ext            | Expanded content         | struct      |                                                                                                                       |                              |
| »camera_mode   | Current camera mode      | enum_int    | {"3":"Panorama"}                                                                                                      |                              |

**Example:**

```
{
    "data": {
        "countdown_time": 4,
        "ext": {
            "camera_mode": 2
        },
        "progress": {
            "current_step": 0,
            "percent": 100
        },
        "result": 0,
        "status": "in_progress"
    },
    "method": "drc_camera_photo_info_push",
    "seq": 1
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#service)Service

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#drc-heart-beat)DRC-heart beat

The seq at the same level as data is an incrementing sequence number that ensures the sequential execution of commands.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** heart_beat

**Data:**

| Column    | Name                             | Type | Constraint                                                    | Description                                                                                                                                                                                                                                                                                                                                                 |
| --------- | -------------------------------- | ---- | ------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| timestamp | Heartbeat transmission timestamp | int  | {"max":"","min":"","step":"","unit_name":"milliseconds / ms"} | The heartbeat protocol allows the users to determine if the DRC (Data Relay Channel) link is active. If no heartbeat response is received within a certain period, error may occur to the DRC link, and a retry can be initiated. If no heartbeat protocol is received for over 1 minute, the device will consider the DRC link idle and exit the DRC link. |

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

| Column    | Name                             | Type | Constraint                                                    | Description                                                                                                                                 |
| --------- | -------------------------------- | ---- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| timestamp | Heartbeat transmission timestamp | int  | {"max":"","min":"","step":"","unit_name":"milliseconds / ms"} | The timestamp when the heartbeat was sent, not the device's time, to help the cloud calculate the delay based on the send-receive interval. |

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

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#subscribe-initial-state-of-drc)Subscribe Initial State of DRC

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_initial_state_subscribe

**Data:** null

**Example:**

```
{
    "data": {},
    "method": "drc_initial_state_subscribe",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_initial_state_subscribe

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_initial_state_subscribe",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#drc-drone-emergency-stop)DRC-drone emergency stop

The seq at the same level as data is an incrementing sequence number that ensures the sequential execution of commands.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drone_emergency_stop

**Data:**

| Column | Name | Type | Constraint                                     | Description |
| ------ | ---- | ---- | ---------------------------------------------- | ----------- |
| null   | null | int  | {"max":"","min":"","step":"","unit_name":null} |             |

**Example:**

```
{
    "data": {},
    "method": "drone_emergency_stop",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drone_emergency_stop

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drone_emergency_stop",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#drc-stick-control)DRC-Stick Control

After establishing the DRC link, the "DRC Stick Control" command can be used to control the aircraft. The sending frequency must be maintained at 5-10Hz to ensure precise control of aircraft movements. This protocol has no acknowledgment mechanism.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** stick_control

**Data:**

| Column   | Name             | Type | constraint                                | Description                                                                                                                                                                                                                                  |
| -------- | ---------------- | ---- | ----------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| roll     | Roll Channel     | int  | {"max":"1684","min":"364","unit_name":""} | Corresponds to RC A-channel (Aileron), controls aircraft roll (manifested as lateral movement). Stick range: 1024±660, 1024=neutral (no action). Increasing values indicate right tilt, decreasing values indicate left tilt.                |
| pitch    | Pitch Channel    | int  | {"max":"1684","min":"364","unit_name":""} | Corresponds to RC E-channel (Elevator), controls aircraft pitch (manifested as longitudinal movement). Stick range: 1024±660, 1024=neutral (no action). Increasing values indicate forward dive, decreasing values indicate backward tilt.   |
| throttle | Throttle Channel | int  | {"max":"1684","min":"364","unit_name":""} | Corresponds to RC T-channel (Throttle), controls aircraft altitude. Stick range: 1024±660, 1024=hover state. Increasing values indicate ascent, decreasing values indicate descent.                                                          |
| yaw      | Yaw Channel      | int  | {"max":"1684","min":"364","unit_name":""} | Corresponds to RC R-channel (Rudder), controls aircraft yaw (manifested as rotation). Stick range: 1024±660, 1024=neutral (no action). Increasing values indicate clockwise rotation, decreasing values indicate counter-clockwise rotation. |

**Example:**

```
{
    "seq": 1,
    "method": "stick_control",
    "data": {
        "roll": 1024,
        "pitch": 1024,
        "throttle": 1024,
        "yaw": 1024
    }
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-lens-distortion-correction)Payload Control - Lens Distortion Correction

Enable users to eliminate vignetting in wide-angle view. Lens distortion correction for Matrice 3D wide-angle mode. ("seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".), H30 not support this function

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_dewarping_set

**Data:**

| Column          | Name                                         | Type        | Constraint                   | Description                                                                                                                                                                                                         |
| --------------- | -------------------------------------------- | ----------- | ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index   | Camera enumeration                           | text        | {"length":""}                | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type     | Camera type                                  | enum_string | {"wide":"Wide-angle"}        | Camera type enumeration                                                                                                                                                                                             |
| dewarping_state | Whether to enable lens distortion correction | enum_int    | {"0":"Disable","1":"Enable"} | Enable or disable lens distortion correction                                                                                                                                                                        |

**Example:**

```
{
    "data": {
        "camera_type": "wide",
        "dewarping_state": 1,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_dewarping_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_dewarping_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_dewarping_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-mechanical-shutter)Payload Control - Mechanical Shutter

Support for users to manually close the mechanical shutter to prolong the lifespan of the equipment. Mechanical shutter switch for Matrice 3D wide-angle mode. ("seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".), H30 not support this function

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_mechanical_shutter_set

**Data:**

| Column                   | Name                                 | Type        | Constraint                   | Description                                                                                                                                                                                                        |
| ------------------------ | ------------------------------------ | ----------- | ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| payload_index            | Camera enumeration                   | text        | {"length":""}                | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}.Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type              | Camera type                          | enum_string | {"wide":"Wide-angle"}        | Camera type enumeration                                                                                                                                                                                            |
| mechanical_shutter_state | Whether to enable mechanical shutter | enum_int    | {"0":"Disable","1":"Enable"} | Enable or disable mechanical shutter                                                                                                                                                                               |

**Example:**

```
{
    "data": {
        "camera_type": "wide",
        "mechanical_shutter_state": 1,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_mechanical_shutter_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_mechanical_shutter_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_mechanical_shutter_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-iso-setting)Payload Control - ISO Setting

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_iso_set

**Data:**

| Column        | Name                     | Type        | Constraint                                                                                                                                                                                                                | Description                                                                                                                                                                                                         |
| ------------- | ------------------------ | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value | text        | {"length":""}                                                                                                                                                                                                             | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type              | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                                                                                                                                                                       | Camera type enumeration                                                                                                                                                                                             |
| iso_value     | ISO value                | enum_int    | {"0":"ISO_AUTO","2":"ISO_50","3":"ISO_100","4":"ISO_200","5":"ISO_400","6":"ISO_800","7":"ISO_1600","8":"ISO_3200","9":"ISO_6400","10":"ISO_12800","11":"ISO_25600","12":"ISO_51200","13":"ISO_102400","255":"ISO_FIXED"} | Depending on the aircraft model, the range may vary.                                                                                                                                                                |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "iso_value": 5,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_iso_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_iso_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_iso_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-camera-shutter)Payload Control - Camera Shutter

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_shutter_set

**Data:**

| Column        | Name                  | Type        | Constraint                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   | Description                                                                                                                                                                                                         |
| ------------- | --------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration    | text        | {"length":""}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type           | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Camera type enumeration                                                                                                                                                                                             |
| shutter_value | Shutter speed setting | enum_int    | {"0":"1/8000 s","1":"1/6400 s","2":"1/6000 s","3":"1/5000 s","4":"1/4000 s","5":"1/3200 s","6":"1/3000 s","7":"1/2500 s","8":"1/2000 s","9":"1/1600 s","10":"1/1500 s","11":"1/1250 s","12":"1/1000 s","13":"1/800 s","14":"1/725 s","15":"1/640 s","16":"1/500 s","17":"1/400 s","18":"1/350 s","19":"1/320 s","20":"1/250 s","21":"1/240 s","22":"1/200 s","23":"1/180 s","24":"1/160 s","25":"1/125 s","26":"1/120 s","27":"1/100 s","28":"1/90 s","29":"1/80 s","30":"1/60 s","31":"1/50 s","32":"1/40 s","33":"1/30 s","34":"1/25 s","35":"1/20 s","36":"1/15 s","37":"1/12.5 s","38":"1/10 s","39":"1/8 s","40":"1/6.25 s","41":"1/5 s","42":"1/4 s","43":"1/3 s","44":"1/2.5 s","45":"1/2 s","46":"1/1.67 s","47":"1/1.25 s","48":"1.0 s","49":"1.3 s","50":"1.6 s","51":"2.0 s","52":"2.5 s","53":"3.0 s","54":"3.2 s","55":"4.0 s","56":"5.0 s","57":"6.0 s","58":"7.0 s","59":"8.0 s","60":"Auto"} | The range varies depending on the model.                                                                                                                                                                            |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "payload_index": "89-0-0",
        "shutter_value": 5

    },
    "method": "drc_camera_shutter_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_shutter_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_shutter_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-camera-aperture)Payload Control - Camera Aperture

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_aperture_value_set

**Data:**

| Column         | Name               | Type        | Constraint                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Description                                                                                                                                                                                                         |
| -------------- | ------------------ | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index  | Camera enumeration | text        | {"length":""}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type    | Camera type        | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Camera type enumeration                                                                                                                                                                                             |
| aperture_value | Aperture value     | enum_int    | {"0":"F_AUTO","50":"F0_5","70":"F0_7","80":"F0_8","90":"F0_9","95":"F0_95","100":"F1_0","110":"F1_1","120":"F1_2","140":"F1_4","160":"F1_6","170":"F1_7","180":"F1_8","200":"F2","220":"F2_2","240":"F2_4","250":"F2_5","280":"F2_8","320":"F3_2","330":"F3_3","340":"F3_4","350":"F3_5","400":"F4","440":"F4_4","450":"F4_5","480":"F4_8","500":"F5","560":"F5_6","630":"F6_3","670":"F6_7","680":"F6_8","710":"F7_1","800":"F8","900":"F9","950":"F9_5","960":"F9_6","1000":"F10","1100":"F11","1300":"F13","1400":"F14","1600":"F16","1800":"F18","1900":"F19","2000":"F20","2200":"F22","2500":"F25","2700":"F27","2900":"F29","3200":"F32","3600":"F36","3800":"F38","4000":"F40","4500":"F45","5100":"F51","5400":"F54","5700":"F57","6400":"F64","7200":"F72","7600":"F76","8000":"F80","9000":"F90","10700":"F107","12800":"F128","18000":"F180","25600":"F256"} | Depending on the aircraft model, the range may vary.                                                                                                                                                                |

**Example:**

```
{
    "data": {
        "aperture_value": 0,
        "camera_type": "zoom",
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_aperture_value_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_aperture_value_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_aperture_value_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#flight-control-discreet-mode)Flight Control - Discreet Mode

Set the aircraft's discreet mode to turn off all aircraft lights. (The "seq" is an incrementing sequence number that ensures commands are executed in order, and it is at the same level as "data".)

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_stealth_state_set

**Data:**

| Column        | Name          | Type     | Constraint                   | Description         |
| ------------- | ------------- | -------- | ---------------------------- | ------------------- |
| stealth_state | Discreet mode | enum_int | {"0":"Disable","1":"Enable"} | Discreet mode state |

**Example:**

```
{
    "data": {
        "stealth_state": 0
    },
    "method": "drc_stealth_state_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_stealth_state_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_stealth_state_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#beacons-settings)Beacons Settings

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_night_lights_state_set

**Data:**

| Column             | Name  | Type     | Constraint                   | Description |
| ------------------ | ----- | -------- | ---------------------------- | ----------- |
| night_lights_state | State | enum_int | {"0":"Disable","1":"Enable"} |             |

**Example:**

```
{
    "data": {
        "night_lights_state": 0
    },
    "method": "drc_night_lights_state_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_night_lights_state_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_night_lights_state_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-set-timed-shot)Payload Control - Set Timed Shot

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_interval_photo_set

**Data:**

| Column        | Name                        | Type        | constraint                                                                                                         | Description                                                                                                                                                                                                         |
| ------------- | --------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration          | text        | {"length":""}                                                                                                      | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| interval      | Time interval of Timed Shot | enum_string | {"1":"1s","2":"2s","3":"3s","5":"5s","7":"7s","10":"10s","15":"15s","20":"20s","30":"30s","60":"60s","0.7":"0.7s"} | The available range of timed photo intervals varies with different photo sizes. For example, when the photo size is set to 8K, the timed photo interval cannot be set to 0.7s or 1s.                                |

**Example:**

```
{
    "data": {
        "interval": 1,
        "payload_index": "89-0-0"
    },
    "method": "drc_interval_photo_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_interval_photo_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_interval_photo_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-photo-storage-setting)Payload Control - Photo Storage Setting

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_photo_storage_set

**Data:**

| Column                 | Name                  | Type  | Constraint                     | Description                                                                                                                                                                                                         |
| ---------------------- | --------------------- | ----- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index          | Camera enumeration    | text  | {"length":""}                  | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| photo_storage_settings | Photo storage setting | array | {"size": -, "item_type": text} | Capturing storage types are {current, vision, ir}. Multiple selections available based on the models.                                                                                                               |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0",
        "photo_storage_settings": [
            "current",
            "ir",
            "vision"
        ]
    },
    "method": "drc_photo_storage_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_photo_storage_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_photo_storage_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-video-storage-setting)Payload Control - Video Storage Setting

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_video_storage_set

**Data:**

| Column                 | Name               | Type  | Constraint                     | Description                                                                                                                                                                                                         |
| ---------------------- | ------------------ | ----- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index          | Camera enumeration | text  | {"length":""}                  | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| video_storage_settings | Video storage      | array | {"size": -, "item_type": text} | Video storage types are {current, vision, ir}. Multiple selections available based on the models.                                                                                                                   |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0",
        "video_storage_settings": [
            "current",
            "ir",
            "vision"
        ]
    },
    "method": "drc_video_storage_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_video_storage_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_video_storage_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-video-resolution-settings)Payload Control - Video Resolution Settings

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_video_resolution_set

**Data:**

| Column           | Name               | Type        | Constraint                             | Description                                                                                                                                                                                                         |
| ---------------- | ------------------ | ----------- | -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index    | Camera enumeration | text        | {"length":""}                          | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| video_resolution | Video resolution   | enum_string | { "0": "1920x1080", "1": "3840x2160" } | The visible light camera supports resolutions of 1920x1080 and 3840x2160. The infrared camera only supports 640x512 and cannot be adjusted.                                                                         |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0",
        "video_resolution": 0
    },
    "method": "drc_video_resolution_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_video_resolution_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_video_resolution_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-link-zoom)Payload Control - Link Zoom

Infrared link zoom is only supported on the Matrice 3TD model. (The "seq" is an incrementing sequence number that ensures commands are executed in order, and it is at the same level as "data".)

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_linkage_zoom_set

**Data:**

| Column        | Name               | Type | Constraint                     | Description                                                                                                                                                                                                         |
| ------------- | ------------------ | ---- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text | {"length":""}                  | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| state         | Link Zoom status   | bool | {"0":"Disabled","1":"Enabled"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0",
        "state": 0
    },
    "method": "drc_linkage_zoom_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_linkage_zoom_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_linkage_zoom_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#emergency-landing)Emergency Landing

When the aircraft is more than 5 meters away from the dock, triggering an emergency landing will cause the aircraft to land on the ground. Use with caution. (The "seq" is an incrementing sequence number that ensures commands are executed in order, and it is at the same level as "data".)

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_emergency_landing

**Data:** null

**Example:**

```
{
    "data": {},
    "method": "drc_emergency_landing",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_emergency_landing

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_emergency_landing",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#emergency-stop)Emergency Stop

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drone_emergency_stop

**Data:** null

**Example:**

```
{
    "data": {},
    "method": "drone_emergency_stop",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drone_emergency_stop

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drone_emergency_stop",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#forced-landing)Forced Landing

When forced landing is enabled, the aircraft will descend to the ground. Use with caution. (The "seq" is an incrementing sequence number that ensures commands are executed in order, and it is at the same level as "data".)

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_force_landing

**Data:** null

**Example:**

```
{
    "data": {},
    "method": "drc_force_landing",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_force_landing

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_force_landing",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-switch-camera-mode)Payload control - Switch Camera Mode

"seq" is an incrementing sequence number ensuring that the instructions are executed in order. It is at the same level as "data".

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_mode_switch

**Data:**

| Column        | Name               | Type     | Constraint                                                                              | Description                                                                                                                                                                                                         |
| ------------- | ------------------ | -------- | --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text     | {"length":""}                                                                           | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_mode   | Mode               | enum_int | {"0":"Capturing","1":"Recording","2":"Smart Low-Light","3":"Panorama","4":"Timed Shot"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "camera_mode": 2,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_mode_switch",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_mode_switch

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_mode_switch",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-start-recording)Payload control - Start Recording

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_recording_start

**Data:**

| Column        | Name               | Type | Constraint | Description                                                                                                                                                                                                         |
| ------------- | ------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text |            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_recording_start",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_recording_start

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_recording_start",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-stop-recording)Payload control - Stop Recording

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_recording_stop

**Data:**

| Column        | Name               | Type | Constraint | Description                                                                                                                                                                                                         |
| ------------- | ------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text |            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_recording_stop",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_recording_stop

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_recording_stop",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-screen-drag-control)Payload control - Screen Drag Control

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_screen_drag

**Data:**

| Column        | Name                                                                                      | Type   | Constraint                                                                                                                                          | Description                                                                                                                                                                                                         |
| ------------- | ----------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration                                                                        | text   |                                                                                                                                                     | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool   | {"false":"Only gimbal turns, the aircraft body does not turn","true":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |                                                                                                                                                                                                                     |
| pitch_speed   | Gimbal pitch speed                                                                        | double | {"unit_name":"radians per second / rad/s"}                                                                                                          | Gimbal pitch speed                                                                                                                                                                                                  |
| yaw_speed     | Gimbal yaw speed                                                                          | double | {"unit_name":"radians per second / rad/s"}                                                                                                          | Gimbal yaw speed, effective only when the aircraft head is not locked.                                                                                                                                              |

**Example:**

```
{
    "data": {
        "locked": false,
        "payload_index": "89-0-0",
        "pitch_speed": 0.6,
        "yaw_speed": -0.6
    },
    "method": "drc_camera_screen_drag",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_screen_drag

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_screen_drag",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-double-tap-to-aim)Payload control - Double tap to AIM

The double-tap AIM function allows you to double-tap on a target point within the camera's field of view, making that point the center of the camera's field of view.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_aim

**Data:**

| Column        | Name                                                                                      | Type        | Constraint                                                                                                                                          | Description                                                                                                                                                                                                         |
| ------------- | ----------------------------------------------------------------------------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration                                                                        | text        |                                                                                                                                                     | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type                                                                               | enum_string | {"ir":"Infrared","wide":"Wide-angle","zoom":"Zoom"}                                                                                                 | Camera type enumeration                                                                                                                                                                                             |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool        | {"false":"Only gimbal turns, the aircraft body does not turn","true":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |                                                                                                                                                                                                                     |
| x             | Target coordinates x                                                                      | double      | {"max":1,"min":0}                                                                                                                                   | Target coordinate x, with the upper-left corner of the lens as the coordinate center, and the horizontal direction as x.                                                                                            |
| y             | Target coordinates y                                                                      | double      | {"max":1,"min":0}                                                                                                                                   | Target coordinates y, with the upper-left corner of the lens as the coordinate center, and the vertical direction as y.                                                                                             |

**Example:**

```
{
    "data": {
        "camera_type": "wide",
        "locked": false,
        "payload_index": "89-0-0",
        "x": 0.8310580204778157,
        "y": 0.32272727272727275
    },
    "method": "drc_camera_aim",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_aim

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_aim",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-zoom)Payload control - Zoom

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_focal_length_set

**Data:**

| Column        | Name               | Type        | Constraint                                          | Description                                                                                                                                                                                                         |
| ------------- | ------------------ | ----------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text        |                                                     | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type        | enum_string | {"ir":"Infrared","wide":"Wide-angle","zoom":"Zoom"} | Camera type enumeration                                                                                                                                                                                             |
| zoom_factor   | Zoom factor        | double      | {"max":200,"min":2}                                 | Zoom factor, 2-200 for visible light, 2-20 for infrared.                                                                                                                                                            |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "payload_index": "89-0-0",
        "zoom_factor": 7
    },
    "method": "drc_camera_focal_length_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_focal_length_set

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_focal_length_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-reset-gimbal)Payload control - Reset gimbal

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_gimbal_reset

**Data:**

| Column        | Name                                      | Type     | Constraint                                                                         | Description                                                                                                                                                                                                                        |
| ------------- | ----------------------------------------- | -------- | ---------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | {_{type-subtype-gimbalindex}__aembLbhPpc} | text     |                                                                                    | Payload ID and camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| reset_mode    | Reset mode type                           | enum_int | {"0":"Reset","1":"Downward","2":"Gimbal pan recenter","3":"Gimbal pitch downward"} |                                                                                                                                                                                                                                    |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0",
        "reset_mode": 1
    },
    "method": "drc_gimbal_reset",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_gimbal_reset

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_gimbal_reset",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-look-at)Payload control - Look At

The Look At function instructs the aircraft to turn from its current orientation to a point specified by actual latitude, longitude, and altitude. For the M30/M30T model, it is recommended to use the method of locking the head. An exception may occur if the gimbal rotates only when the gimbal limit angle is reached.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_look_at

**Data:**

| Column        | Name                                                                                      | Type   | Constraint                                                                                                                                          | Description                                                                                                                                                                                                         |
| ------------- | ----------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration                                                                        | text   |                                                                                                                                                     | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool   | {"false":"Only gimbal turns, the aircraft body does not turn","true":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |                                                                                                                                                                                                                     |
| latitude      | Target point latitude (in degree values)                                                  | double | {"max":90,"min":-90}                                                                                                                                | Angle value. Negative for south latitude, and positive for north latitude. Accurate to six decimal places.                                                                                                          |
| longitude     | Target point longitude (in degree values)                                                 | double | {"max":180,"min":-180}                                                                                                                              | Angle value. Positive for east longitude, negative for west longitude. Accurate to six decimal places.                                                                                                              |
| height        | Target point height                                                                       | float  | {"max":10000,"min":2,"step":0.1,"unit_name":"Meters / m"}                                                                                           | Target point height (ellipsoid height)                                                                                                                                                                              |

**Example:**

```
{
    "data": {
        "height": 24.838796976545467,
        "latitude": 22.908061229971967,
        "locked": false,
        "longitude": 113.70510712582023,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_look_at",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_look_at

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_look_at",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-split-screen)Payload control - Split Screen

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_screen_split

**Data:**

| Column        | Name                           | Type | Constraint | Description                                                                                                                                                                                                         |
| ------------- | ------------------------------ | ---- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration             | text |            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| enable        | Whether to enable split screen | bool |            | Enable or disable split screen                                                                                                                                                                                      |

**Example:**

```
{
    "data": {
        "enable": true,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_screen_split",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_screen_split

**Data:**

| Column | Name        | Type | Constraint | Description                  |
| ------ | ----------- | ---- | ---------- | ---------------------------- |
| result | Return code | int  |            | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "enable": true,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_screen_split",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-subject-zoom)Payload control - subject zoom

The subject zoom function enables to select and frame a target area within the camera's field of view. When activated, the camera screen will automatically switch to the target frame, zoom in and adjust the gimbal to the center.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_frame_zoom

**Data:**

| Column        | Name                                                                                      | Type        | Constraint                                                                                                                                   | Description |
| ------------- | ----------------------------------------------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| payload_index | Camera enumeration                                                                        | text        | {"length":""}                                                                                                                                |             |
| camera_type   | Camera type                                                                               | enum_string | {"ir":"Infrared","wide":"Wide-angle","zoom":"Zoom"}                                                                                          |             |
| locked        | Whether the relative relationship between the aircraft's heading and the gimbal is locked | bool        | {"0":"Only gimbal turns, the aircraft body does not turn","1":"Lock the aircraft's heading. The gimbal and the aircraft body turn together"} |             |
| x             | Upper-left coordinate x of the target frame                                               | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |
| y             | Upper-left coordinate y of the target frame                                               | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |
| width         | Width of the target frame                                                                 | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |
| height        | Height of target frame                                                                    | float       | {"max":"1","min":"0","step":"0.000001","unit_name":"None / "}                                                                                |             |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "height": 0.5,
        "locked": true,
        "payload_index": "89-0-0",
        "width": 0.8,
        "x": 0.5,
        "y": 0.5
    },
    "method": "drc_camera_frame_zoom",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_frame_zoom

**Data:**

| Column | Name        | Type | Constraint                                     | Description |
| ------ | ----------- | ---- | ---------------------------------------------- | ----------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} |             |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_frame_zoom",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-infrared-temperature-measurement-area-setting)Payload control - Infrared temperature measurement area setting

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_ir_metering_area_set

**Data:**

| Column        | Name                                                                          | Type   | Constraint                               | Description                                                                                                                                                                                                         |
| ------------- | ----------------------------------------------------------------------------- | ------ | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration                                                            | text   | {"length":""}                            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| x             | Coordinate x of the left and upper corner of the temperature measurement area | double | {"max":1,"min":"","step":"","unit_name"} | The left and upper corner of the lens is the coordinate center. The horizontal direction is x.                                                                                                                      |
| y             | Coordinate y of the left and upper corner of the temperature measurement area | double | {"max":1,"min":"","step":"","unit_name"} | The left and upper corner of the lens is the coordinate center. The vertical direction is y.                                                                                                                        |
| width         | Width of the temperature measurement area                                     | double | {"max":1,"min":"","step":"","unit_name"} |                                                                                                                                                                                                                     |
| height        | Height of the temperature measurement area                                    | double | {"max":1,"min":"","step":"","unit_name"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "height": 0.2,
        "payload_index": "89-0-0",
        "width": 0.2,
        "x": 0.5,
        "y": 0.5
    },
    "method": "drc_ir_metering_area_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_ir_metering_area_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_ir_metering_area_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-infrared-temperature-measurement-point-setting)Payload control - Infrared temperature measurement point setting

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_ir_metering_point_set

**Data:**

| Column        | Name                                       | Type   | Constraint                               | Description                                                                                                                                                                                                         |
| ------------- | ------------------------------------------ | ------ | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration                         | text   | {"length":""}                            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| x             | Temperature measurement point coordinate x | double | {"max":1,"min":"","step":"","unit_name"} | The left and upper corner of the lens is the coordinate center. The horizontal direction is x.                                                                                                                      |
| y             | Temperature measurement point coordinate y | double | {"max":1,"min":"","step":"","unit_name"} | The left and upper corner of the lens is the coordinate center. The vertical direction is y.                                                                                                                        |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0",
        "x": 0.5,
        "y": 0.5
    },
    "method": "drc_ir_metering_point_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_ir_metering_point_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_ir_metering_point_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-infrared-temperature-measurement-mode-setting)Payload control - Infrared temperature measurement mode setting

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_ir_metering_mode_set

**Data:**

| Column        | Name               | Type     | Constraint                                                                                                   | Description                                                                                                                                                                                                         |
| ------------- | ------------------ | -------- | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration | text     | {"length":""}                                                                                                | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| mode          | Measurement mode   | enum_int | {"0":"Close temperature measurement","1":"Point temperature measurement","2":"Area temperature measurement"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "mode": 1,
        "payload_index": "89-0-0"
    },
    "method": "drc_ir_metering_mode_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_ir_metering_mode_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_ir_metering_mode_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-spot-af)Payload control - Spot AF

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_point_focus_action

**Data:**

| Column        | Name                         | Type        | Constraint                               | Description                                                                                                                                                                                                         |
| ------------- | ---------------------------- | ----------- | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value     | text        | {"length":""}                            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type                  | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}      | Camera type enumeration. Note, this parameter only supports to be set on zoom lens when using Matrice 30 series aircraft                                                                                            |
| x             | Focusing point coordinate x  | double      | {"max":1,"min":"","step":"","unit_name"} | The left and upper corner of the lens is the coordinate center. The horizontal direction is x.                                                                                                                      |
| y             | Focusing point coordinates y | double      | {"max":1,"min":"","step":"","unit_name"} | The left and upper corner of the lens is the coordinate center. The vertical direction is y.                                                                                                                        |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "payload_index": "89-0-0",
        "x": 0.5,
        "y": 0.5
    },
    "method": "drc_camera_point_focus_action",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_point_focus_action

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_point_focus_action",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-camera-focus-value-setting)Payload control - Camera focus value setting

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_focus_value_set

**Data:**

| Column        | Name                     | Type        | Constraint                                     | Description                                                                                                                                                                                                         |
| ------------- | ------------------------ | ----------- | ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value | text        | {"length":""}                                  | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type              | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}            | Camera type enumeration. Note, this parameter only supports to be set on zoom lens when using Matrice 30 series aircraft                                                                                            |
| focus_value   | Focus value              | int         | {"max":"","min":"","step":"","unit_name":null} | Focus value. The range can be get from the aircraft properties, zoom_max_focus_value and zoom_min_focus_value.                                                                                                      |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "focus_value": 11,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_focus_value_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_focus_value_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_focus_value_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-camera-focus-mode-setting)Payload control - Camera focus mode setting

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_focus_mode_set

**Data:**

| Column        | Name                     | Type        | Constraint                          | Description                                                                                                                                                                                                         |
| ------------- | ------------------------ | ----------- | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value | text        | {"length":""}                       | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type              | enum_string | {"wide":"Wide-angle","zoom":"Zoom"} | Camera type enumeration. Note, this parameter only supports to be set on zoom lens when using Matrice 30 series aircraft                                                                                            |
| focus_mode    | Focus mode               | enum_int    | {"0":"MF","1":"AFS","2":"AFC"}      |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "focus_mode": 0,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_focus_mode_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_focus_mode_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_focus_mode_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-camera-exposure-value-adjustment)Payload control - Camera exposure value adjustment

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_exposure_set

**Data:**

| Column         | Name               | Type        | Constraint                                                                                                                                                                                                                                                                                                                                                                                                                             | Description                                                                                                                                                                                                         |
| -------------- | ------------------ | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index  | Camera enumeration | text        | {"length":""}                                                                                                                                                                                                                                                                                                                                                                                                                          | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type    | Camera type        | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                                                                                                                                                                                                                                                                                                                                                                                    |                                                                                                                                                                                                                     |
| exposure_value | Exposure value     | enum_string | {"1":"-5.0EV","2":"-4.7EV","3":"-4.3EV","4":"-4.0EV","5":"-3.7EV","6":"-3.3EV","7":"-3.0EV","8":"-2.7EV","9":"-2.3EV","10":"-2.0EV","11":"-1.7EV","12":"-1.3EV","13":"-1.0EV","14":"-0.7EV","15":"-0.3EV","16":"0EV","17":"0.3EV","18":"0.7EV","19":"1.0EV","20":"1.3EV","21":"1.7EV","22":"2.0EV","23":"2.3EV","24":"2.7EV","25":"3.0EV","26":"3.3EV","27":"3.7EV","28":"4.0EV","29":"4.3EV","30":"4.7EV","31":"5.0EV","255":"FIXED"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "exposure_value": 8,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_exposure_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_exposure_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_exposure_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-camera-exposure-mode-setting)Payload control - Camera exposure mode setting

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_exposure_mode_set

**Data:**

| Column        | Name                     | Type        | Constraint                                                                        | Description                                                                                                                                                                                                         |
| ------------- | ------------------------ | ----------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value | text        | {"length":""}                                                                     | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| camera_type   | Camera type              | enum_string | {"wide":"Wide-angle","zoom":"Zoom"}                                               | Camera type enumeration                                                                                                                                                                                             |
| exposure_mode | Exposure Mode            | enum_int    | {"1":"Auto","2":"Shutter priority","3":"Aperture priority","4":"Manual exposure"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "camera_type": "zoom",
        "exposure_mode": 1,
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_exposure_mode_set",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_exposure_mode_set

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_exposure_mode_set",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-stop-capturing)Payload control - Stop capturing

Command to stop capturing. Currently, only panorama mode is supported.

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_photo_stop

**Data:**

| Column        | Name                     | Type | Constraint    | Description                                                                                                                                                                                                         |
| ------------- | ------------------------ | ---- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value | text | {"length":""} | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_photo_stop",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_photo_stop

**Data:**

| Column | Name        | Type | Constraint                                     | Description                  |
| ------ | ----------- | ---- | ---------------------------------------------- | ---------------------------- |
| result | Return code | int  | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_photo_stop",
    "seq": 1
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/remote-control.html#payload-control-start-capturing)Payload control - Start capturing

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_camera_photo_take

**Data:**

| Column        | Name                     | Type | Constraint    | Description                                                                                                                                                                               |
| ------------- | ------------------------ | ---- | ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration value | text | {"length":""} | Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |

**Example:**

```
{
    "data": {
        "payload_index": "89-0-0"
    },
    "method": "drc_camera_photo_take",
    "seq": 1
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_camera_photo_take

**Data:**

| Column | Name        | Type        | Constraint                                     | Description                                                                                                                                                                                                                                               |
| ------ | ----------- | ----------- | ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| result | Return code | int         | {"max":"","min":"","step":"","unit_name":null} | Non-zero represents an error                                                                                                                                                                                                                              |
| status | Task state  | enum_string | {"in_progress":"Executing"}                    | When panorama photo capturing or other persistent photo capturing is in progress, state information will be uploaded. It represents there are further persistent progress event uploading. For details, please refer to camera_photo_take_progress event. |

**Example:**

```
{
    "data": {
        "result": 0
    },
    "method": "drc_camera_photo_take",
    "seq": 1
}
```
