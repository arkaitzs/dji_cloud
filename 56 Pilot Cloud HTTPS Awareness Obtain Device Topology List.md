# Obtain Device Topology List

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/40.situation-awareness/10.obtain-device-topology-list.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#obtain-device-topology-list)Obtain Device Topology List

In the first connection, DJI Pilot 2 will send out a http request to obtain all devices list and topology list. On the server end, it needs to synchronize the device list to the DJI Pilot 2. Also, if it receives a instruction of device online/offline/update from WebSocket, it needs the same interface to request the update of device topology list. `GET /manage/api/v1/workspaces/{workspace_id}/devices/topologies`

### Parameters

| Name         | In     | Type   | Required | Description  |
| ------------ | ------ | ------ | -------- | ------------ |
| workspace_id | path   | string | true     | workspace id |
| x-auth-token | header | string | true     | access token |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                                           |
| ------ | ------------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [tsa.GetWebPrjDeviceForOpenPlatformRsp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schematsa.getwebprjdeviceforopenplatformrsp) |

> Example responses

```
{
    "code": 0,
    "message": "success",
    "data": {
        "list": [{
            "hosts": [{
                "sn": "drone01",
                "device_model": {
                    "key": "0-60-0",
                    "domain": "0",
                    "type": "60",
                    "sub_type": "0"
                },
                "online_status": true,
                "device_callsign": "Rescue aircraft",
                "user_id": "string",
                "user_callsign": "string",
                "icon_urls": {
                    "normal_icon_url": "resource://pilot/drawable/tsa_aircraft_others_normal",
                    "selected_icon_url": "resource://pilot/drawable/tsa_aircraft_others_pressed"
                }
            }],
            "parents": [{
                "sn": "rc02",
                "online_status": true,
                "device_model": {
                    "key": "2-56-0",
                    "domain": "2",
                    "type": "56",
                    "sub_type": "0"
                },
                "device_callsign": "Remote controller",
                "user_id": "string",
                "user_callsign": "string",
                "icon_urls": {
                    "normal_icon_url": "resource://pilot/drawable/tsa_aircraft_others_normal",
                    "selected_icon_url": "resource://pilot/drawable/tsa_aircraft_others_pressed"
                }
            }]
        }]
    }
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schemas)Schemas

## tsa.GetWebPrjDeviceForOpenPlatformRsp

```
{
    "code":0,
    "message":"success",
    "data":{
        "list":[
            {
                "hosts":[
                    {
                        "device_callsign":"string",
                        "device_model":{
                            "key":"string",
                            "domain":"string",
                            "type":"string",
                            "sub_type":"string"
                        },
                        "icon_urls":{
                            "normal_icon_url":"string",
                            "selected_icon_url":"string"
                        },
                        "online_status":true,
                        "sn":"string",
                        "user_callsign":"string",
                        "user_id":"string"
                    }
                ],
                "parents":[
                    {
                        "device_callsign":"string",
                        "device_model":{
                            "key":"string",
                            "domain":"string",
                            "type":"string",
                            "sub_type":"string"
                        },
                        "icon_urls":{
                            "normal_icon_url":"string",
                            "selected_icon_url":"string"
                        },
                        "online_status":true,
                        "sn":"string",
                        "user_callsign":"string",
                        "user_id":"string"
                    }
                ]
            }
        ]
    }
}
```

*Properties*

| Name | Type                                                                                                                                                                                       | Required | Restrictions | Description |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | ----------- |
| list | [[tsa.DeviceTopoRsp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schematsa.devicetoporsp)] | false    | none         | none        |

## tsa.DeviceTopoRsp

```
{
  "hosts": [
    {
      "device_callsign": "string",
      "device_model": {
        "key":"string",
        "domain":"string",        
        "type":"string",
        "sub_type":"string"
      },   
      "icon_urls": {
        "normal_icon_url": "string",
        "selected_icon_url": "string"
      },
      "online_status": true,
      "sn": "string",
      "user_callsign": "string",
      "user_id": "string"
    }
  ],
  "parents": [
    {
      "device_callsign": "string",
       "device_model": {
         "key":"string",
         "domain":"string",        
         "type":"string",
         "sub_type":"string"
       },
      "icon_urls": {
        "normal_icon_url": "string",
        "selected_icon_url": "string"
      },
      "online_status": true,
      "sn": "string",
      "user_callsign": "string",
      "user_id": "string"
    }
  ]
}
```

*Properties*

| Name    | Type                                                                                                                                                                                                     | Required | Restrictions | Description                        |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ---------------------------------- |
| hosts   | [[tsa.TopoHostDeviceRsp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schematsa.topohostdevicersp)]       | false    | none         | drone device topology collection   |
| parents | [[tsa.TopoGatewayDeviceRsp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schematsa.topogatewaydevicersp)] | false    | none         | gateway device topology collection |

## tsa.TopoGatewayDeviceRsp

```
{
  "device_callsign": "string",
  "device_model": {
    "key":"string",
    "domain":"string",        
    "type":"string",
    "sub_type":"string"
  },
   "icon_urls": {
    "normal_icon_url": "string",
    "selected_icon_url": "string"
  },
  "online_status": true,
  "sn": "string",
  "user_callsign": "string",
  "user_id": "string"
}
```

*Properties*

| Name                | Type                                                                                                                                                                                         | Required | Restrictions | Description                                                                                                                                    |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| device_callsign     | string                                                                                                                                                                                       | false    | none         | device callsign                                                                                                                                |
| device_model        | [tsa.DeviceModelEnum](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schematsa.devicemodelenum) | false    | none         | device enum object                                                                                                                             |
| icon_urls           | object                                                                                                                                                                                       | false    | none         | Custom icon; if empty, it will be automatically loaded by device_model type. If not empty, display the picture according to this configuration |
| » normal_icon_url   | string                                                                                                                                                                                       | false    | none         | icon in normal state                                                                                                                           |
| » selected_icon_url | string                                                                                                                                                                                       | false    | none         | icon in the selected state                                                                                                                     |
| online_status       | boolean                                                                                                                                                                                      | false    | none         | device online status                                                                                                                           |
| sn                  | string                                                                                                                                                                                       | false    | none         | serial number                                                                                                                                  |
| user_callsign       | string                                                                                                                                                                                       | false    | none         | user callsign                                                                                                                                  |
| user_id             | string                                                                                                                                                                                       | false    | none         | user id                                                                                                                                        |

## tsa.TopoHostDeviceRsp

```
{
  "device_callsign": "string",
  "device_model": {
    "key":"string",
    "domain":"string",        
    "type":"string",
    "sub_type":"string"
  },
  "icon_urls": {
    "normal_icon_url": "string",
    "selected_icon_url": "string"
  },
  "online_status": true,
  "sn": "string",
  "user_callsign": "string",
  "user_id": "string"
}
```

*Properties*

| Name                | Type                                                                                                                                                                                         | Required | Restrictions | Description                                                                                                                                    |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| device_callsign     | string                                                                                                                                                                                       | false    | none         | device callsign                                                                                                                                |
| device_model        | [tsa.DeviceModelEnum](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html#schematsa.devicemodelenum) | false    | none         | device enum object                                                                                                                             |
| icon_urls           | object                                                                                                                                                                                       | false    | none         | custom icon; if empty, it will be automatically loaded by device_model type. If not empty, display the picture according to this configuration |
| » normal_icon_url   | string                                                                                                                                                                                       | false    | none         | icon in normal state                                                                                                                           |
| » selected_icon_url | string                                                                                                                                                                                       | false    | none         | icon in the selected state                                                                                                                     |
| online_status       | boolean                                                                                                                                                                                      | false    | none         | device online status                                                                                                                           |
| sn                  | string                                                                                                                                                                                       | false    | none         | serial number                                                                                                                                  |
| user_callsign       | string                                                                                                                                                                                       | false    | none         | user callsign                                                                                                                                  |
| user_id             | string                                                                                                                                                                                       | false    | none         | user id                                                                                                                                        |

## tsa.DeviceModelEnum

```
{
  "domain": "string",
  "key": "string",
  "sub_type": "string",
  "type": "text"
}
```

*Properties*

| Name     | Type   | Required | Restrictions | Description          |
| -------- | ------ | -------- | ------------ | -------------------- |
| domain   | string | false    | none         | product enum         |
| key      | string | false    | none         | product enum id      |
| sub_type | string | false    | none         | product enum subtype |
| type     | string | false    | none         | product enum type    |
