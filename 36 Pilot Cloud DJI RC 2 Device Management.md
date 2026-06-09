# Device Management

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/00.mqtt/40.dji-rc-plus-2/10.device.md) 

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/device.html#event)Event

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/dji-rc-plus-2/device.html#device-topology-update)Device topology update

**Topic:** thing/product/*{gateway_sn}*/status

**Direction:** up

**Method:** update_topo

**Data:**

| Column         | Name                                  | Type  | constraint                       | Description                                                                                                       |
| -------------- | ------------------------------------- | ----- | -------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| type           | Gateway device product type           | int   |                                  | Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| sub_type       | Gateway sub-device product subtype    | int   |                                  | Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| device_secret  | Gateway device key                    | text  |                                  |                                                                                                                   |
| nonce          | nonce                                 | text  |                                  |                                                                                                                   |
| thing_version  | Thing model version of gateway device | text  |                                  |                                                                                                                   |
| sub_devices    | Sub-device list                       | array | {"size": 1, "item_type": struct} |                                                                                                                   |
| »sn            | Sub-device serial number (SN)         | text  |                                  |                                                                                                                   |
| »type          | Sub-device product type               | int   |                                  | Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| »sub_type      | Sub-device product subtype            | int   |                                  | Refer to [Supported Products](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html). |
| »device_secret | Sub-device key                        | text  |                                  |                                                                                                                   |
| »nonce         | nonce                                 | text  |                                  |                                                                                                                   |
| »thing_version | Thing model version of sub-device     | text  |                                  |                                                                                                                   |
