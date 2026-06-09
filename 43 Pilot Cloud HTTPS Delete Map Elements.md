# Delete Map Elements

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/10.map-elements/40.delete.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/delete.html#delete-map-elements)Delete Map Elements

`DELETE /map/api/v1/workspaces/{workspace_id}/elements/{id}`

### Parameters

| Name         | In     | Type    | Required | Description  |
| ------------ | ------ | ------- | -------- | ------------ |
| id           | path   | integer | true     | element id   |
| workspace_id | path   | string  | true     | workspace id |
| x-auth-token | header | string  | true     | access token |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                     |
| ------ | ------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [map.SwagUUIDResp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/delete.html#schemamap.swaguuidresp) |

> Example responses

```
{
    "code":0
       "data":{
        "id":"94c51c50-f111-45e8-ac8c-4f96c93ced44"
    },
    "message": "success"
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/delete.html#schemas)Schemas

## map.SwagUUIDResp

```
{
  "code": 0,
  "data": {
    "id": "string"
  },
  "message": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                               | Required | Restrictions | Description       |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------------- |
| code    | integer                                                                                                                                            | true     | none         | error code        |
| data    | [map.UUIDResp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/delete.html#schemamap.uuidresp) | true     | none         | none              |
| message | string                                                                                                                                             | true     | none         | error description |

## map.UUIDResp

```
{
  "id": "string"
}
```

*Properties*

| Name | Type   | Required | Restrictions | Description |
| ---- | ------ | -------- | ------------ | ----------- |
| id   | string | true     | none         | element id  |
