# Create Map Elements

2025-03-19

4.3 Ratings

3 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/10.map-elements/10.create.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#create-map-elements)Create Map Elements

`POST /map/api/v1/workspaces/{workspace_id}/element-groups/{group_id}/elements`

### Parameters

| Name         | In     | Type                                                                                                                                                                   | Required | Description      |
| ------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------------- |
| group_id     | path   | string                                                                                                                                                                 | true     | element group id |
| workspace_id | path   | string                                                                                                                                                                 | true     | workspace id     |
| x-auth-token | header | string                                                                                                                                                                 | true     | access token     |
| body param   | body   | [map.ElementCreateInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#schemamap.elementcreateinput) | true     | body param       |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                     |
| ------ | ------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [map.SwagUUIDResp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#schemamap.swaguuidresp) |

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

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#schemas)Schemas

## map.ElementCreateInput

```
{
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
  }
}
```

*Properties*

| Name     | Type                                                                                                                                                       | Required | Restrictions | Description     |
| -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | --------------- |
| id       | string                                                                                                                                                     | true     | none         | element id      |
| name     | string                                                                                                                                                     | true     | none         | element name    |
| resource | [map.ResourceItem](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#schemamap.resourceitem) | true     | none         | resource object |

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
| content | [map.Content](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#schemamap.content) | false    | none         | resource content object                                         |
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
| data    | [map.UUIDResp](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/map-elements/create.html#schemamap.uuidresp) | true     | none         | none              |
| message | string                                                                                                                                             | true     | none         | error description |

## map.UUIDResp

```
{
  "id": "string"
}
```

*Properties*

| Name | Type   | Required | Restrictions | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ---- | ------ | -------- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| id   | string | true     | none         | element idCreate Map Elements<br/>2025-03-19<br/>4.3 Ratings<br/>3 customers rated<br/>Github Edit <br/>#Create Map Elements<br/>POST /map/api/v1/workspaces/{workspace_id}/element-groups/{group_id}/elements<br/><br/>Parameters<br/>Name	In	Type	Required	Description<br/>group_id	path	string	true	element group id<br/>workspace_id	path	string	true	workspace id<br/>x-auth-token	header	string	true	access token<br/>body param	body	map.ElementCreateInput	true	body param<br/>Responses<br/>Status	Meaning	Description	Schema<br/>200	OK	OK	map.SwagUUIDResp<br/>Example responses<br/><br/>{<br/>	"code":0<br/>   	"data":{<br/>    	"id":"94c51c50-f111-45e8-ac8c-4f96c93ced44"<br/>    },<br/>    "message": "success"<br/>}<br/><br/>#Schemas<br/>map.ElementCreateInput<br/><br/>{<br/>  "id": "string",<br/>  "name": "string",<br/>  "resource": {<br/>    "content": {<br/>      "geometry": {<br/>        "coordinates": [<br/>          null<br/>        ],<br/>        "type": "text"<br/>      },<br/>      "properties": {<br/>        "clampToGround": true,<br/>        "color": "string"<br/>      },<br/>      "type": "text"<br/>    },<br/>    "type": 0<br/>  }<br/>}<br/><br/>Properties<br/><br/>Name	Type	Required	Restrictions	Description<br/>id	string	true	none	element id<br/>name	string	true	none	element name<br/>resource	map.ResourceItem	true	none	resource object<br/>map.ResourceItem<br/><br/>{<br/>  "content": {<br/>    "geometry": {<br/>      "coordinates": [<br/>        null<br/>      ],<br/>      "type": "text"<br/>    },<br/>    "properties": {<br/>      "clampToGround": true,<br/>      "color": "string"<br/>    },<br/>    "type": "text"<br/>  },<br/>  "type": 0<br/>}<br/><br/>Properties<br/><br/>Name	Type	Required	Restrictions	Description<br/>content	map.Content	false	none	resource content object<br/>type	integer	false	none	resource type<br/>* 0 - pin point<br/>* 1 - line<br/>* 2 - polygon<br/>map.Content<br/><br/>{<br/>  "geometry": {<br/>    "coordinates": [<br/>      null<br/>    ],<br/>    "type": "text"<br/>  },<br/>  "properties": {<br/>    "clampToGround": true,<br/>    "is3d": false,<br/>    "color": "string"<br/>  },<br/>  "type": "text"<br/>}<br/><br/>Properties<br/><br/>Name	Type	Required	Restrictions	Description<br/>geometry	object	false	none	geojson attribute<br/>» coordinates	[any]	false	none	geojson attribute<br/>» type	string	false	none	geojson attribute<br/>properties	object	false	none	geojson attribute<br/>» clampToGround	boolean	false	none	whether it is on the ground<br/>» is3d	boolean	false	none	whether is it a spatial line surface<br/>» color	string	false	none	supported colors<br/>* BLUE: 0x2D8CF0<br/>* GREEN - 0x19BE6B<br/><br/>* YELLOW - 0xFFBB00<br/><br/>* ORANGE - 0xB620E0<br/><br/>* RED - 0xE23C39<br/>* PURPLE - 0x212121<br/>type	string	false	none	geojson attribute<br/>map.SwagUUIDResp<br/><br/>{<br/>  "code": 0,<br/>  "data": {<br/>    "id": "string"<br/>  },<br/>  "message": "string"<br/>}<br/><br/>Properties<br/><br/>Name	Type	Required	Restrictions	Description<br/>code	integer	true	none	error code<br/>data	map.UUIDResp	true	none	none<br/>message	string	true	none	error description<br/>map.UUIDResp<br/><br/>{<br/>  "id": "string"<br/>}<br/><br/>Properties<br/><br/>Name	Type	Required	Restrictions	Description<br/>id	string	true	none	element id |
