# Device Management

2025-03-19

5 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/00.mqtt/20.rc-pro/10.device.md) 

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/device.html#event)Event

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-pro/device.html#device-topology-update)Device topology update

**Topic:** sys/product/*{gateway_sn}*/status

**Direction:** up

**Method:** update_topo

**Data:**

| Column         | Name                                           | Type   | constraint                       | Description                                                                                                       |
| -------------- | ---------------------------------------------- | ------ | -------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| domain         | Gateway device namespace                       | string |                                  | Reference to [Product Support](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html) |
| type           | Gateway device product type                    | int    |                                  | Reference to [Product Support](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html) |
| sub_type       | Gateway sub-device product subtype             | int    |                                  | Reference to [Product Support](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html) |
| device_secret  | Gateway device key                             | text   |                                  |                                                                                                                   |
| nonce          | nonce                                          | text   |                                  |                                                                                                                   |
| thing_version  | Thing model version of gateway device          | text   |                                  |                                                                                                                   |
| sub_devices    | Sub-device list                                | array  | {"size": 1, "item_type": struct} |                                                                                                                   |
| »sn            | Sub-device serial number (SN)                  | text   |                                  |                                                                                                                   |
| »domain        | Sub-device namespace                           | string |                                  | Reference to [Product Support](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html) |
| »type          | Sub-device product type                        | int    |                                  | Reference to [Product Support](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html) |
| »sub_type      | Sub-device product subtype                     | int    |                                  | Reference to [Product Support](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html) |
| »index         | Channel index connecting to the gateway device | string |                                  |                                                                                                                   |
| »device_secret | Sub-device key                                 | text   |                                  |                                                                                                                   |
| »nonce         | nonce                                          | text   |                                  |                                                                                                                   |
| »thing_version | Thing model version of sub-device              | text   |                                  |                                                                                                                   |

```
//sub_devices online  
{
   "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
   "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
   "method": "update_topo",
   "timestamp": 1234567890123,
   "data": {
       "domain": "3",
       "type": 119,
       "sub_type": 0,
       "device_secret": "secret",
       "nonce": "nonce",
       "thing_version": "1.1.2",
       "sub_devices": [
           {
               "sn": "drone001",
               "domain": "0",
               "type": 60,
               "sub_type": 0,
               "index": "A",
               "device_secret": "secret",
               "nonce": "nonce",
               "thing_version": "1.1.2"
           }
       ]
   }
}

//sub_devices offline 
{
   "tid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
   "bid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
   "method": "update_topo",
   "timestamp": 1234567890123,
   "data": {
       "domain": "3",
       "type": 119,
       "sub_type": 0,
       "device_secret":"secret",
       "nonce":"nonce",
       "thing_version": "1.1.2",
       "sub_devices":[]
   }
} 
```

**Topic:** sys/product/*{gateway_sn}*/status_reply

**Direction:** down

**Method:** update_topo

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
    "timestamp": 1654070968655,
    "method": "update_topo"
}
```
