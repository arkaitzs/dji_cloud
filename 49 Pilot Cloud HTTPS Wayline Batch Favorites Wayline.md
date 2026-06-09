# Batch Favorites Wayline

2025-03-19

5 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/60.collect-waypointfile-in-batch.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/collect-waypointfile-in-batch.html#batch-favorites-waypoints)Batch Favorites Waypoints

`POST /wayline/api/v1/workspaces/{workspace_id}/favorites`

### Parameters

| Name         | In     | Type   | Required | Description     |
| ------------ | ------ | ------ | -------- | --------------- |
| workspace_id | path   | string | true     | workspace id    |
| x-auth-token | header | string | true     | access token    |
| id           | path   | Array  | true     | wayline file ID |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                                           |
| ------ | ------------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [wayline_service.CreateFavoriteOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/collect-waypointfile-in-batch.html#schemawayline_service.createfavoriteoutput) |

> Example responses

```
{
    "code":0,
       "data":{},
    "message": "success"
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/collect-waypointfile-in-batch.html#schemas)Schemas

## wayline_service.CreateFavoriteOutput

```
{
  "code": 0,
  "data": {
    "id": [
      "string"
    ]
  },
  "message": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                                                                                                           | Required | Restrictions | Description |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | ----------- |
| code    | integer                                                                                                                                                                                                                        | false    | none         | error code  |
| data    | [wayline_service.CreateFavoriteInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/collect-waypointfile-in-batch.html#schemawayline_service.createfavoriteinput) | false    | none         | none        |
| message | string                                                                                                                                                                                                                         | false    | none         | description |
