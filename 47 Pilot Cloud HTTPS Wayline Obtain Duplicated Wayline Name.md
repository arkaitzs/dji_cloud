# Obtain Duplicated Wayline Name

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/40.get-duplicated-waypointfile-name.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/get-duplicated-waypointfile-name.html#get-duplicated-waypoints-name)Get Duplicated Waypoints Name

`GET /wayline/api/v1/workspaces/:workspace_id/waylines/duplicate-names`

### Parameters

| Name         | In     | Type          | Required | Description                    |
| ------------ | ------ | ------------- | -------- | ------------------------------ |
| workspace_id | path   | string        | true     | workspace id                   |
| name         | query  | array[string] | true     | waypoints file name collection |
| x-auth-token | header | string        | true     | access token                   |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                                    |
| ------ | ------------------------------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [wayline.GetDuplicateNamesOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/get-duplicated-waypointfile-name.html#schemawayline.getduplicatenamesoutput) |

> Example

```
{
    "code": 0,
    "message": "string",
    "data": ["name1", "name2"]
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/get-duplicated-waypointfile-name.html#schemas)Schemas

## wayline.GetDuplicateNamesOutput

```
{
  "code": 0,
  "data": [
    "string"
  ],
  "message": "string"
}
```

*Properties*

| Name    | Type          | Required | Restrictions | Description                    |
| ------- | ------------- | -------- | ------------ | ------------------------------ |
| code    | integer       | false    | none         | error code                     |
| data    | array[string] | false    | none         | duplicate file name collection |
| message | string        | false    | none         | description                    |
