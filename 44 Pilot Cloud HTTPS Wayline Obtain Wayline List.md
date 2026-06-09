# Obtain Wayline List

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/10.obtain-waypointfile-list.md) 

`GET /wayline/api/v1/workspaces/{workspace_id}/waylines`

### Parameters

| Name              | In     | Type           | Required | Description                                                             |
| ----------------- | ------ | -------------- | -------- | ----------------------------------------------------------------------- |
| workspace_id      | path   | string         | true     | workspace id                                                            |
| favorited         | query  | boolean        | false    | whether to favorite                                                     |
| order_by          | query  | string         | false    | order (xxx_column desc or xxx_column asc                                |
| page              | query  | integer        | false    | current page                                                            |
| page_size         | query  | integer        | false    | page size                                                               |
| template_type     | query  | array[integer] | false    | waypoints template type collection                                      |
| x-auth-token      | header | string         | true     | access token                                                            |
| action_type       | query  | integer        | false    | 1: Enable AI Spot-Check wayline. Without this field means all waylines. |
| drone_model_keys  | query  | array[string]  | false    | Selected aircraft models                                                |
| payload_model_key | query  | array[string]  | false    | Selected payload models                                                 |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                |
| ------ | ------------------------------------------------------- | ----------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [wayline.GetWaylinesOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-waypointfile-list.html#schemawayline.getwaylinesoutput) |

> Example responses

```
{
    "code":0,
    "message":"success",
    "data":{
        "list":[
            {
                "id":"uuid",
                "drone_model_key":"0-67-0",
                "favorited":false,
                "name":"New wayline 1",
                "payload_model_keys":[
                    "1-53-0"
                ],
                "template_types":[
                    0
                ],
                "action_type": 0,
                "update_time":1637158501230,
                "user_name":"string",
                "start_wayline_point": {
                  "start_latitude": 22.5799601837589,
                  "start_lontitude": 113.942744030171
                  }
            }
        ],
        "pagination":{
            "page":1,
            "page_size":9,
            "total":10
        }
    }
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-waypointfile-list.html#schemas)Schemas

## wayline.GetWaylinesOutput

```
{
  "code": 0,
  "data": {
    "list": [
      {
        "drone_model_key": "string",
        "favorited": true,
        "id": "string",
        "name": "string",
        "payload_model_keys": [
          "string"
        ],
        "template_types": [
          0
        ],
        "action_type": 0,
        "update_time": 0,
        "user_name": "string",
        "start_wayline_point": {
          "start_latitude": 22.5799601837589,
          "start_lontitude": 113.942744030171
          }
      }
    ],
    "pagination": {
      "page": 0,
      "page_size": 0,
      "total": 0
    }
  },
  "message": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                                                                                          | Required | Restrictions | Description |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------- |
| code    | integer                                                                                                                                                                                                       | false    | none         | error code  |
| data    | [wayline.GetWaylinesOutputData](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-waypointfile-list.html#schemawayline.getwaylinesoutputdata) | false    | none         | none        |
| message | string                                                                                                                                                                                                        | false    | none         | description |

## wayline.GetWaylinesOutputData

```
{
  "list": [
    {
      "drone_model_key": "string",
      "favorited": true,
      "id": "string",
      "name": "string",
      "payload_model_keys": [
        "string"
      ],
      "template_types": [
        0
      ],
      "action_type": 0,
      "update_time": 0,
      "user_name": "string",
      "start_wayline_point": {
          "start_latitude": 22.5799601837589,
          "start_lontitude": 113.942744030171
          }
    }
  ],
  "pagination": {
    "page": 0,
    "page_size": 0,
    "total": 0
  }
}
```

*Properties*

| Name       | Type                                                                                                                                                                                        | Required | Restrictions | Description |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------- |
| list       | [[wayline.AppFileItem](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-waypointfile-list.html#schemawayline.appfileitem)] | false    | none         | none        |
| pagination | [api_render.PagyInfo](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-waypointfile-list.html#schemaapi_render.pagyinfo)   | false    | none         | none        |

## wayline.AppFileItem

```
{
  "drone_model_key": "string",
  "favorited": true,
  "id": "string",
  "name": "string",
  "payload_model_keys": [
    "string"
  ],
  "template_types": [
    0
  ],
  "action_type": 0,
  "update_time": 0,
  "user_name": "string",
  "start_wayline_point": {
    "start_latitude": 22.5799601837589,
    "start_lontitude": 113.942744030171
    }
}
```

*Properties*

| Name                | Type         | Required | Restrictions | Description                      |
| ------------------- | ------------ | -------- | ------------ | -------------------------------- |
| drone_model_key     | string       | false    | none         | drone device product enum        |
| favorited           | boolean      | false    | none         | whether to favorite              |
| id                  | string       | false    | none         | waypoints file id                |
| name                | string       | false    | none         | waypoints file name              |
| payload_model_keys  | [string]     | false    | none         | payload device product enum      |
| template_types      | [integer]    | false    | none         | waypoints template collection    |
| update_time         | integer      | false    | none         | update time (millisecond)        |
| user_name           | string       | false    | none         | uploader                         |
| start_wayline_point | array[float] | false    | none         | Start point of wayline           |
| start_latitude      | float        | false    | none         | Latitude of wayline start point  |
| start_lontitude     | float        | false    | none         | Longitude of wayline start point |

## api_render.PagyInfo

```
{
  "page": 0,
  "page_size": 0,
  "total": 0
}
```

*Properties*

| Name      | Type    | Required | Restrictions | Description  |
| --------- | ------- | -------- | ------------ | ------------ |
| page      | integer | false    | none         | current page |
| page_size | integer | false    | none         | page size    |
| total     | integer | false    | none         | total        |
