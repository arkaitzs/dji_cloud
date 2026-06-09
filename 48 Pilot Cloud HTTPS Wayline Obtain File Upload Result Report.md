# Obtain File Upload Result Report

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/50.waypointfile-upload-result-report.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/waypointfile-upload-result-report.html#report-waypoints-upload-result)Report Waypoints Upload Result

`POST /wayline/api/v1/workspaces/{workspace_id}/upload-callback`

### Parameters

| Name         | In     | Type                                                                                                                                                                                                               | Required | Description  |
| ------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ |
| workspace_id | path   | string                                                                                                                                                                                                             | true     | workspace id |
| x-auth-token | header | string                                                                                                                                                                                                             | true     | access token |
| body         | body   | [wayline.UploadCallbackInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/waypointfile-upload-result-report.html#schemawayline.uploadcallbackinput) | true     | body param   |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                               |
| ------ | ------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [wayline.BaseResponse](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/waypointfile-upload-result-report.html#schemawayline.baseresponse) |

> Example responses

```
{
    "code":0
       "data": {},
    "message": "success"
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/waypointfile-upload-result-report.html#schemas)Schemas

## wayline.UploadCallbackInput

```
{
  "metadata": {
    "drone_model_key": "string",
    "payload_model_keys": [
      "string"
    ],
    "template_types": [
      0
    ]
  },
  "name": "string",
  "object_key": "string",
}
```

*Properties*

| Name       | Type                                                                                                                                                                                         | Required | Restrictions | Description             |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------------------- |
| metadata   | [wayline.Metadata](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/waypointfile-upload-result-report.html#schemawayline.metadata) | false    | none         | waypoints file metadata |
| name       | string                                                                                                                                                                                       | false    | none         | waypoints file name     |
| object_key | string                                                                                                                                                                                       | true     | none         | object key              |

## wayline.Metadata

```
{
  "drone_model_key": "string",
  "payload_model_keys": [
    "string"
  ],
  "template_types": [
    0
  ]
}
```

*Properties*

| Name               | Type      | Required | Restrictions | Description                   |
| ------------------ | --------- | -------- | ------------ | ----------------------------- |
| drone_model_key    | string    | false    | none         | device product enum           |
| payload_model_keys | [string]  | false    | none         | payload product enum          |
| template_types     | [integer] | false    | none         | waypoints template collection |
