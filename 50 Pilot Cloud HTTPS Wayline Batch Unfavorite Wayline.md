# Batch Unfavorite Wayline

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/70.cancel-collect.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/cancel-collect.html#batch-unfavorites-waypoints)Batch Unfavorites Waypoints

`DELETE /wayline/api/v1/workspaces/{workspace_id}/favorites`

### Parameters

| Name         | In     | Type          | Required | Description                  |
| ------------ | ------ | ------------- | -------- | ---------------------------- |
| workspace_id | path   | string        | true     | workspace id                 |
| id           | query  | array[string] | false    | waypoints file id collection |
| x-auth-token | header | string        | true     | access token                 |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                            |
| ------ | ------------------------------------------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [wayline.BaseResponse](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/cancel-collect.html#schemawayline.baseresponse) |

> Example responses

```
{
    "code":0
       "data":{},
    "message": "success"
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/cancel-collect.html#schemas)Schemas

## wayline.BaseResponse

```
{
  "code": 0,
  "data": null,
  "message": "string"
}
```

*Properties*

| Name    | Type    | Required | Restrictions | Description |
| ------- | ------- | -------- | ------------ | ----------- |
| code    | integer | false    | none         | error code  |
| data    | any     | false    | none         | none        |
| message | string  | false    | none         | description |
