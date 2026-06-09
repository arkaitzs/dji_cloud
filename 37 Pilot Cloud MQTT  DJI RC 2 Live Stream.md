# Live Stream

2025-12-03

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/00.mqtt/40.dji-rc-plus-2/20.live.md) 

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/live.html#service)Service

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/live.html#set-live-streaming-lens)Set live streaming lens

**Topic:** thing/product/*{gateway_sn}*/drc/down

**Direction:** down

**Method:** drc_live_lens_change

**Data:**

| Column        | Name                           | Type        | Constraint                                               | Description                                                                                                                                                                                                         |
| ------------- | ------------------------------ | ----------- | -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| payload_index | Camera enumeration             | text        | {"length":""}                                            | Camera enumeration value. Non-standard device_mode_key. The format is {type-subtype-gimbalindex}. Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| video_type    | Live streaming video lens type | enum_string | {"thermal":"Infrared","wide":"Wide-angle","zoom":"Zoom"} |                                                                                                                                                                                                                     |

**Example:**

```
{
    "data": {
        "payload_index": "80-0-0",
        "video_type": "zoom"
    },
    "timestamp:": 1654070968655,
    "method": "drc_live_lens_change"
}
```

**Topic:** thing/product/*{gateway_sn}*/drc/up

**Direction:** up

**Method:** drc_live_lens_change

**Data:** null

**Example:**

```
{
    "data": {
        "result": 0
    },
    "timestamp:": 1654070968655,
    "method": "drc_live_lens_change"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/live.html#set-live-streaming-quality)Set live streaming quality

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** live_set_quality

**Data:**

| Column        | Name                                  | Type     | Constraint                                                                                                | Description                                                                                                                                                                                                                                                                                                                                          |
| ------------- | ------------------------------------- | -------- | --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| video_id      | ID of the live streaming video stream | text     |                                                                                                           | Identifier for a certain video stream, in the format of *{sn}/{camera_index}/{video_index}*. Here, {sn} represents the serial number of the video source device. *{camera_index}* is the camera index, formatted as *{type-subtype-gimbalindex}*. *{video_index}* is the stream index that can be selected for the video source at the camera level. |
| video_quality | Live streaming quality                | enum_int | {"0":"Adaptive","1":"Smooth","2":"Standard definition","3":"High definition","4":"Ultra-high definition"} | The resolutions and bitrates for different video qualities are as follows: Fluent: 960 * 540, 512Kbps; Standard Definition (SD): 1280 * 720, 1Mbps; High Definition (HD): 1280 * 720, 1.5Mbps; Ultra High Definition (UHD): 1920 * 1080, 3Mbps.                                                                                                      |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "video_id": "1ZNDH1D0010098/39-0-7/normal-0",
        "video_quality": 4
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp:": 1654070968655,
    "method": "live_set_quality"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** live_set_quality

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
    "timestamp:": 1654070968655,
    "method": "live_set_quality"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/live.html#stop-live-streaming)Stop live streaming

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** live_stop_push

**Data:**

| Column   | Name                                  | Type | Constraint | Description                                                                                                                                                                                                                                                                                                                                          |
| -------- | ------------------------------------- | ---- | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| video_id | ID of the live streaming video stream | text |            | Identifier for a certain video stream, in the format of *{sn}/{camera_index}/{video_index}*. Here, {sn} represents the serial number of the video source device. *{camera_index}* is the camera index, formatted as *{type-subtype-gimbalindex}*. *{video_index}* is the stream index that can be selected for the video source at the camera level. |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "video_id": "1ZNDH1D0010098/42-0-0/zoom-0"
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp:": 1654070968655,
    "method": "live_stop_push"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** live_stop_push

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
    "timestamp:": 1654070968655,
    "method": "live_stop_push"
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/live.html#start-live-streaming)Start live streaming

**Topic:** thing/product/*{gateway_sn}*/services

**Direction:** down

**Method:** live_start_push

**Data:**

| Column        | Name                                  | Type     | Constraint                                                                                                | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| ------------- | ------------------------------------- | -------- | --------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| url_type      | Live streaming protocol type          | enum_int | {"0":"Agora","1":"RTMP","3":"GB28181","4":"WebRTC"}                                                       | WebRTC only supports interaction via the WHIP protocol.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| url           | Live streaming parameters             | text     | {"length":""}                                                                                             | RTMP: (rtmp://xxxxxxx, example: rtmp://192.168.1.1:8080/live). GB28181: (serverIP&serverPort&serverID&agentID&agentPassword&localPort&channel, example: serverIP=192.168.1.1&serverPort=8080&serverID=34000000000000000000&agentID=300000000010000000000&agentPassword=0000000&localPort=7060&channel=340000000000000000000). Agora: (channel&sn&token&uid, example: channel=1ZNDH360010162_39-0-7&sn=1ZNDH360010162&token=006dca67721582a48768ec4d817b7b25a86IAB4cw2JgN6iX8BpTPdc3e4S1Iendz94IFJ56aSXKvzAJei27MqF2zyCIgCLIIoBt41+YAQAAQC3jX5gAgC3jX5gAwC3jX5gBAC3jX5g&uid=50000). Note: Agora-generated tokens may contain special characters like '+', which need to be URL-encoded once. Otherwise, the Pilot side may encounter parsing errors. WebRTC: (Example: http://192.168.1.1:8080/rtc/v1/whip/?app=live&stream=livestream) |
| video_id      | ID of the live streaming video stream | text     | {"length":""}                                                                                             | Identifier for a certain video stream, in the format of *{sn}/{camera_index}/{video_index}*. Here, {sn} represents the serial number of the video source device. *{camera_index}* is the camera index, formatted as *{type-subtype-gimbalindex}*. *{video_index}* is the stream index that can be selected for the video source at the camera level.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| video_quality | Live streaming quality                | enum_int | {"0":"Adaptive","1":"Smooth","2":"Standard definition","3":"High definition","4":"Ultra-high definition"} | The resolutions and bitrates for different video qualities are as follows: Fluent: 960 * 540, 512Kbps; Standard Definition (SD): 1280 * 720, 1Mbps; High Definition (HD): 1280 * 720, 1.5Mbps; Ultra High Definition (UHD): 1920 * 1080, 3Mbps.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |

**Example:**

```
{
    "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "data": {
        "url": "channel=1ZNDH1D0010098_39-0-7&sn=1ZNDH1D0010098&token=006dca67721582a48768ec4d817b7b25a86IADk%2Fcm%2Fdv%2BHY6qT%2FAKM6y7TcUe4lXNvZpycH7vUMAlM6pFALUKF2zyCIgA82pQE8cCoYAQAAQDxwKhgAgDxwKhgAwDxwKhgBADxwKhg&uid=50000",
        "url_type": 0,
        "video_id": "1ZNDH1D0010098/39-0-7/normal-0",
        "video_quality": 0
    },
    "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "timestamp:": 1654070968655,
    "method": "live_start_push"
}
```

**Topic:** thing/product/*{gateway_sn}*/services_reply

**Direction:** up

**Method:** live_start_push

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
    "timestamp:": 1654070968655,
    "method": "live_start_push"
}
```
