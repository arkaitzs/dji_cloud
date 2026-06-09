# Update Map Elements

2025-03-19

4.6 Ratings

3 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/10.map-elements/20.update.md) 

`PUT /map/api/v1/workspaces/{workspace_id}/elements/{id}`

### Parameters

| Name         | In     | Type                                                                                                                                                                   | Required | Description  |
| ------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ |
| workspace_id | path   | string                                                                                                                                                                 | true     | workspace id |
| id           | path   | string                                                                                                                                                                 | true     | element id   |
| x-auth-token | header | string                                                                                                                                                                 | true     | access token |
| body         | body   | [map.ElementUpdateInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/update.html#schemamap.elementupdateinput) | true     | body         |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                     |
| ------ | ------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [map.SwagUUIDResp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/update.html#schemamap.swaguuidresp) |

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

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/update.html#schemas)Schemas

## map.ElementUpdateInput

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
  "name": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                             | Required | Restrictions | Description             |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | ----------------------- |
| content | [map.Content](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/update.html#schemamap.content) | false    | none         | resource content object |
| name    | string                                                                                                                                           | false    | none         | element name            |

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

| Name            | Type    | Required | Restrictions                                                                                                                                                    | Description                          |
| --------------- | ------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| geometry        | object  | false    | none                                                                                                                                                            | geojson attribute                    |
| » coordinates   | [any]   | false    | none                                                                                                                                                            | geojson attribute                    |
| » type          | string  | false    | none                                                                                                                                                            | geojson attribute                    |
| properties      | object  | false    | none                                                                                                                                                            | geojson attribute                    |
| » clampToGround | boolean | false    | none                                                                                                                                                            | whether it is on the ground          |
| » is3d          | boolean | false    | none                                                                                                                                                            | whether is it a spatial line surface |
| » color         | string  | false    | supported colors<br>* BLUE: 0x2D8CF0<br>* GREEN - 0x19BE6B<br><br>* YELLOW - 0xFFBB00<br><br>* ORANGE - 0xB620E0<br><br>* RED - 0xE23C39<br>* PURPLE - 0x212121 |                                      |
| type            | string  | false    | none                                                                                                                                                            | geojson attribute                    |

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
| data    | [map.UUIDResp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/update.html#schemamap.uuidresp) | true     | none         | none              |
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
