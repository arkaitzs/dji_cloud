# Obtain Map Elements

2025-03-19

5 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/10.map-elements/30.obtain.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#get-map-elements)Get Map Elements

`GET /map/api/v1/workspaces/{workspace_id}/element-groups`

### Parameters

| Name           | In     | Type    | Required | Description                                            |
| -------------- | ------ | ------- | -------- | ------------------------------------------------------ |
| group_id       | query  | string  | false    | element group id                                       |
| is_distributed | query  | boolean | false    | element group distributed status (default value: true) |
| workspace_id   | path   | string  | true     | workspace id                                           |
| x-auth-token   | header | string  | true     | access token                                           |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                             |
| ------ | ------------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [map.SwagTree](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#schemamap.swagtree) |

> Example responses

```
{
    "code":0,
    "message":"success",
    "data":[
        {
            "id":"d275c4e1-d864-4736-8b5d-5f5882ee9bdd",
            "type":1,
            "name":"string",
            "is_lock":false,
            "create_time":1637158501230,
            "elements":[
                {
                    "id":"6d5da7b4-ac39-42bf-9580-4c8a94c11989",
                    "name":"string",
                    "create_time":1637158501230,
                    "update_time":1637158501230,
                    "resource":{
                        "type":0,
                        "user_name":"name",
                        "content":{
                            "type":"Feature",
                            "properties":{
                                "color":"#0091FF",
                                "clampToGround":false
                            },
                            "geometry":{
                                "type":"Point",
                                "coordinates":[
                                    -112.49344909640939,
                                    48.18734850103778,
                                    40.2
                                ]
                            }
                        }
                    }
                },
                {
                    "id":"6d5da7b4-ac39-42bf-9580-4c8a94c11988",
                    "name":"string",
                    "create_time":1637158501230,
                    "update_time":1637158501230,
                    "resource":{
                        "type":0,
                        "user_name":"string",
                        "content":{
                            "type":"Feature",
                            "properties":{
                                "color":"#0091FF",
                                "clampToGround":false
                            },
                            "geometry":{
                                "type":"LineString",
                                "coordinates":[
                                    [
                                        -112.49344909640939,
                                        48.18734850103778
                                    ],
                                    [
                                        -104.47267952618783,
                                        46.60029335386577
                                    ],
                                    [
                                        -109.20285386177949,
                                        42.63945392538244
                                    ],
                                    [
                                        -113.51847222524307,
                                        43.059268439796575
                                    ]
                                ]
                            }
                        }
                    }
                },
                {
                    "id":"6d5da7b4-ac39-42bf-9580-4c8a94c11987",
                    "name":"string",
                    "create_time":1637158501230,
                    "update_time":1637158501230,
                    "resource":{
                        "type":0,
                        "user_name":"string",
                        "content":{
                            "type":"Feature",
                            "properties":{
                                "color":"#0091FF",
                                "clampToGround":false
                            },
                            "geometry":{
                                "type":"Polygon",
                                "coordinates":[
                                    [
                                        [
                                            -112.49344909640939,
                                            48.18734850103778
                                        ],
                                        [
                                            -104.47267952618783,
                                            46.60029335386577
                                        ],
                                        [
                                            -109.20285386177949,
                                            42.63945392538244
                                        ],
                                        [
                                            -113.51847222524307,
                                            43.059268439796575
                                        ]
                                    ]
                                ]
                            }
                        }
                    }
                }
            ]
        }
    ]
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#schemas)Schemas

## map.SwagTree

```
{
  "code": 0,
  "data": [
    {
      "create_time": 0,
      "elements": [
        {
          "create_time": 0,
          "id": "string",
          "name": "string",
          "resource": {
            "content": {
              "geometry": {
                "coordinates": [
                  null
                ],
                "type": "text"
              },
              "properties": {
                "clampToGround": true,
                "color": "string"
              },
              "type": "text"
            },
            "type": 0
          },
          "update_time": 0
        }
      ],
      "id": "string",
      "is_lock": true,
      "name": "string",
      "type": 0
    }
  ],
  "message": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                                                     | Required | Restrictions | Description       |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | ----------------- |
| code    | integer                                                                                                                                                                  | false    | none         | error code        |
| data    | [[map.ElementGroupOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#schemamap.elementgroupoutput)] | false    | none         | none              |
| message | string                                                                                                                                                                   | false    | none         | error description |

## map.ElementGroupOutput

```
{
  "create_time": 0,
  "elements": [
    {
      "create_time": 0,
      "id": "string",
      "name": "string",
      "resource": {
        "content": {
          "geometry": {
            "coordinates": [
              null
            ],
            "type": "text"
          },
          "properties": {
            "clampToGround": true,
            "color": "string"
          },
          "type": "text"
        },
        "type": 0
      },
      "update_time": 0
    }
  ],
  "id": "string",
  "is_lock": true,
  "name": "string",
  "type": 0
}
```

*Properties*

| Name        | Type                                                                                                                                                       | Required | Restrictions | Description                                                                                                                                                                                                                         |
| ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| create_time | integer                                                                                                                                                    | false    | none         | element group create time (millisecond)                                                                                                                                                                                             |
| elements    | [[map.ElementItem](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#schemamap.elementitem)] | false    | none         | collection of elements                                                                                                                                                                                                              |
| id          | string                                                                                                                                                     | false    | none         | element group id                                                                                                                                                                                                                    |
| is_lock     | boolean                                                                                                                                                    | false    | none         | Whether to lock (if locked, the elements under this element group cannot be deleted and modified)                                                                                                                                   |
| name        | string                                                                                                                                                     | false    | none         | element group name                                                                                                                                                                                                                  |
| type        | integer                                                                                                                                                    | false    | none         | Element Group Types:<br>* 0 - Custom Element Group<br>* 1 - Default Element Group<br>* 2 - App Shared Element Group (when type=2, it refers to the App Shared Element Group. By default, Pilot will add maps to this element group) |

## map.ElementItem

```
{
  "create_time": 0,
  "id": "string",
  "name": "string",
  "resource": {
    "content": {
      "geometry": {
        "coordinates": [
          null
        ],
        "type": "text"
      },
      "properties": {
        "clampToGround": true,
        "color": "string"
      },
      "type": "text"
    },
    "type": 0
  },
  "update_time": 0
}
```

*Properties*

| Name        | Type                                                                                                                                                       | Required | Restrictions | Description                       |
| ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | --------------------------------- |
| create_time | integer                                                                                                                                                    | false    | none         | element create time (millisecond) |
| id          | string                                                                                                                                                     | false    | none         | element id                        |
| name        | string                                                                                                                                                     | false    | none         | element name                      |
| resource    | [map.ResourceItem](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#schemamap.resourceitem) | false    | none         | resource object                   |
| update_time | integer                                                                                                                                                    | false    | none         | element update time (millisecond) |

## map.ResourceItem

```
{
  "content": {
    "geometry": {
      "coordinates": [
        null
      ],
      "type": "text"
    },
    "properties": {
      "clampToGround": true,
      "color": "string"
    },
    "type": "text"
  },
  "type": 0
}
```

*Properties*

| Name    | Type                                                                                                                                             | Required | Restrictions | Description                                                     |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | --------------------------------------------------------------- |
| content | [map.Content](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/obtain.html#schemamap.content) | false    | none         | resource content object                                         |
| type    | integer                                                                                                                                          | false    | none         | resource type<br>* 0 - pin point<br>* 1 - line<br>* 2 - polygon |

## map.Content

```
{
  "geometry": {
    "coordinates": [
      null
    ],
    "type": "text"
  },
  "properties": {
    "clampToGround": true,
     "is3d": false,
    "color": "string"
  },
  "type": "text"
}
```

*Properties*

| Name            | Type    | Required | Restrictions | Description                                                                                                                                                     |
| --------------- | ------- | -------- | ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| geometry        | object  | false    | none         | geojson attribute                                                                                                                                               |
| » coordinates   | [any]   | false    | none         | geojson attribute                                                                                                                                               |
| » type          | string  | false    | none         | geojson attribute                                                                                                                                               |
| properties      | object  | false    | none         | geojson attribute                                                                                                                                               |
| » clampToGround | boolean | false    | none         | whether it is on the ground                                                                                                                                     |
| » is3d          | boolean | false    | none         | whether is it a spatial line surface                                                                                                                            |
| » color         | string  | false    | none         | supported colors<br>* BLUE: 0x2D8CF0<br>* GREEN - 0x19BE6B<br><br>* YELLOW - 0xFFBB00<br><br>* ORANGE - 0xB620E0<br><br>* RED - 0xE23C39<br>* PURPLE - 0x212121 |
| type            | string  | false    | none         | geojson attribute                                                                                                                                               |
