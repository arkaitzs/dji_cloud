# Push Message

2025-03-19

3 Ratings

2 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/20.websocket/10.map-elements/10.message-push.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#message-mapelementcreate)Message `mapElementCreate`

`Create Map Elements`

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#payload)Payload

| Name                    | Type    | Description  | Value | Constraints | Notes                                 |
| ----------------------- | ------- | ------------ | ----- | ----------- | ------------------------------------- |
| (root)                  | object  | -            | -     | -           | **additional properties are allowed** |
| biz_code                | string  |              | -     | -           | -                                     |
| version                 | string  |              | -     | -           | -                                     |
| timestamp               | integer | unit: ms     | -     | -           | -                                     |
| data                    | object  | -            | -     | -           | **additional properties are allowed** |
| data.id                 | string  | element id   | -     | -           | -                                     |
| data.group_id           | string  |              | -     | -           | -                                     |
| data.name               | string  | element name | -     | -           | -                                     |
| data.resource           | object  | -            | -     | -           | **additional properties are allowed** |
| data.resource.user_name | string  |              | -     | -           | -                                     |
| data.resource.content   | object  |              | -     | -           | **additional properties are allowed** |
| data.resource.type      | integer |              | -     | -           | -                                     |

> Examples of payload *(generated)*

```
{
    "biz_code":"map_element_create",
    "version":"1.0",
    "timestamp":146052438362,
    "data":{
        "group_id":"string",
        "id":"string",
        "name":"",
        "resource":{
            "user_name":"string",
            "content":{
                "type":"Feature",
                "properties":{
                    "color":"#0091FF"
                },
                "geometry":{
                    "type":"LineString",
                    "coordinates":[
                        [
                            -114.59526255248164,
                            44.52039593722584
                        ],
                        [
                            -96.91234166804537,
                            47.39200791922252
                        ],
                        [
                            -101.53652432009943,
                            39.10142503321269
                        ]
                    ]
                }
            },
            "type":0
        }
    }
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#message-mapelementdelete)Message `mapElementDelete`

`Delete Map Elements`

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#payload-1)Payload

| Name          | Type    | Description | Value | Constraints | Notes                                 |
| ------------- | ------- | ----------- | ----- | ----------- | ------------------------------------- |
| (root)        | object  | -           | -     | -           | **additional properties are allowed** |
| biz_code      | string  |             | -     | -           | -                                     |
| version       | string  |             | -     | -           | -                                     |
| timestamp     | integer | unit: ms    | -     | -           | -                                     |
| data          | object  | -           | -     | -           | **additional properties are allowed** |
| data.id       | string  | element id  | -     | -           | -                                     |
| data.group_id | string  |             | -     | -           | -                                     |

> Examples of payload *(generated)*

```
{
    "biz_code":"map_element_delete",
    "version":"1.0",
    "timestamp":146052438362,
    "data":{
        "id":"string",
        "group_id":"string"
    }
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#message-mapelementupdate)Message `mapElementUpdate`

`Update Map Elements`

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#payload-2)Payload

| Name                    | Type    | Description  | Value | Constraints | Notes                                 |
| ----------------------- | ------- | ------------ | ----- | ----------- | ------------------------------------- |
| (root)                  | object  | -            | -     | -           | **additional properties are allowed** |
| biz_code                | string  |              | -     | -           | -                                     |
| version                 | string  |              | -     | -           | -                                     |
| timestamp               | integer | unit: ms     | -     | -           | -                                     |
| data                    | object  | -            | -     | -           | **additional properties are allowed** |
| data.id                 | string  | element id   | -     | -           | -                                     |
| data.group_id           | string  |              | -     | -           | -                                     |
| data.name               | string  | element name | -     | -           | -                                     |
| data.resource           | object  |              | -     | -           | **additional properties are allowed** |
| data.resource.user_name | string  |              | -     | -           | -                                     |
| data.resource.content   | object  |              | -     | -           | **additional properties are allowed** |
| data.resource.type      | integer |              | -     | -           | -                                     |

> Examples of payload *(generated)*

```
{
    "biz_code":"map_element_update",
    "version":"1.0",
    "timestamp":146052438362,
    "data":{
        "id":"string",
        "name":"string",
        "group_id":"string",
        "resource":{
            "content":{
                "type":"Feature",
                "properties":{
                    "color":"#0091FF"
                },
                "geometry":{
                    "type":"LineString",
                    "coordinates":[
                        [
                            -114.59526255248164,
                            44.52039593722584
                        ],
                        [
                            -96.91234166804537,
                            47.39200791922252
                        ],
                        [
                            -101.53652432009943,
                            39.10142503321269
                        ]
                    ]
                }
            },
            "type":0,
            "user_name":"string"
        }
    }
}
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#message-mapgrouprefresh)Message `mapGroupRefresh`

`Refresh Map Element List`

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/map-elements/message-push.html#payload-3)Payload

| Name                   | Type          | Description | Value | Constraints | Notes                                 |
| ---------------------- | ------------- | ----------- | ----- | ----------- | ------------------------------------- |
| (root)                 | object        | -           | -     | -           | **additional properties are allowed** |
| biz_code               | string        |             | -     | -           | -                                     |
| version                | string        |             | -     | -           | -                                     |
| timestamp              | integer       | unit: ms    | -     | -           | -                                     |
| data                   | object        | -           | -     | -           | **additional properties are allowed** |
| data.ids               | array[string] | -           | -     | -           | -                                     |
| data.ids (single item) | string        | element id  | -     | -           | -                                     |

> Examples of payload *(generated)*

```
{
  "biz_code": "map_group_refresh",
  "version": "1.0",
  "timestamp": 146052438362,
  "data": {
    "ids": [
      "string"
    ]
  }
}
```

How was your reading experience of our documentation?
