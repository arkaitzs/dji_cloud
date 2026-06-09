# Push Message

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/20.websocket/20.situation-awareness/10.message-push.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#message-deviceosd)Message `deviceOsd`

`Situation Awarness`

The server side pushes the telemetry information of all devices in the same workspace to the DJI Pilot 2 side at a fixed frequency, and DJI Pilot 2 will update the device status and location in the map in real time according to the received data.

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#payload)Payload

| Name                       | Type    | Description                | Value | Constraints | Notes                                 |
| -------------------------- | ------- | -------------------------- | ----- | ----------- | ------------------------------------- |
| (root)                     | object  | -                          | -     | -           | **additional properties are allowed** |
| biz_code                   | string  |                            | -     | -           | -                                     |
| version                    | string  |                            | -     | -           | -                                     |
| timestamp                  | integer | unit: ms                   | -     | -           | -                                     |
| data                       | object  | -                          | -     | -           | **additional properties are allowed** |
| data.host                  | object  | -                          | -     | -           | **additional properties are allowed** |
| data.sn                    | string  | device sn                  | -     | -           | **additional properties are allowed** |
| data.host.latitude         | number  |                            | -     | -           | -                                     |
| data.host.longitude        | number  |                            | -     | -           | -                                     |
| data.host.height           | integer |                            | -     | -           | -                                     |
| data.host.attitude_head    | number  |                            | -     | -           | -                                     |
| data.host.elevation        | number  | Relative take-off altitude | -     | -           | -                                     |
| data.host.horizontal_speed | number  |                            | -     | -           | -                                     |
| data.host.vertical_speed   | number  |                            | -     | -           | -                                     |

> Examples of payload

```
{
  "biz_code": "device_osd",
  "version": "1.0",
  "timestamp": 146052438362,
  "data": {
    "host": {
      "latitude": 113.44444,
      "longitude": 23.45656,
      "height": 44.35,
      "attitude_head": 90,
      "elevation": 40,
      "horizontal_speed": 0,
      "vertical_speed": 2.3
    },
    "sn": "string"
  }
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#message-deviceonline)Message `deviceOnline`

`device online`

When the server receives a request for any device in the same workspace to come online, it also broadcasts a push to DJI Pilot 2 via WebSocket, and when DJI Pilot 2 receives the push, it will trigger the "**Obtain Device Topology List**".

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#payload-1)Payload

| Name      | Type    | Description | Value | Constraints | Notes                                 |
| --------- | ------- | ----------- | ----- | ----------- | ------------------------------------- |
| (root)    | object  | -           | -     | -           | **additional properties are allowed** |
| biz_code  | string  |             | -     | -           | -                                     |
| version   | string  |             | -     | -           | -                                     |
| timestamp | integer | unit: ms    | -     | -           | -                                     |
| data      | object  | -           | -     | -           | **additional properties are allowed** |

> Examples of payload

```
{
  "biz_code": "device_online",
  "version": "1.0",
  "timestamp": 146052438362,
  "data": {}
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#message-deviceoffline)Message `deviceOffline`

`device offline`

When the server receives a request to take any device offline in the same workspace, it also broadcasts a device offline push to DJI Pilot 2 via WebSocket, and when DJI Pilot 2 receives the push, it will trigger the "**Obtain Device Topology List**".

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#payload-2)Payload

| Name      | Type    | Description | Value | Constraints | Notes                                 |
| --------- | ------- | ----------- | ----- | ----------- | ------------------------------------- |
| (root)    | object  | -           | -     | -           | **additional properties are allowed** |
| biz_code  | string  |             | -     | -           | -                                     |
| version   | string  |             | -     | -           | -                                     |
| timestamp | integer | unit: ms    | -     | -           | -                                     |
| data      | object  | -           | -     | -           | **additional properties are allowed** |

> Examples of payload

```
{
  "biz_code": "device_offline",
  "version": "1.0",
  "timestamp": 146052438362,
  "data": {}
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#message-deviceupdatetopo)Message `deviceUpdateTopo`

`update device topology`

When the Server receivesa request for updating the device topology of any device in the same workspace, a push of updating the device topology is also broadcast by WebSocket to the Pilot. After the Pilot receives the push, **Obtain Device Topology List** API will be triggered.

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html#payload-3)Payload

| Name      | Type    | Description | Value | Constraints | Notes                                 |
| --------- | ------- | ----------- | ----- | ----------- | ------------------------------------- |
| (root)    | object  | -           | -     | -           | **additional properties are allowed** |
| biz_code  | string  |             | -     | -           | -                                     |
| version   | string  |             | -     | -           | -                                     |
| timestamp | integer | unit: ms    | -     | -           | -                                     |
| data      | object  | -           | -     | -           | **additional properties are allowed** |

> Examples of payload

```
{
  "biz_code":"device_update_topo",
  "version":"1.0",
  "timestamp":146052438362,
  "data":{}
}
```
