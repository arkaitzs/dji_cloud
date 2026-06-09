# App Reports File Upload Result

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/30.media-management/40.mediafile-upload-result-report.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#report-media-upload-result)Report Media Upload Result

`POST /media/api/v1/workspaces/{workspace_id}/upload-callback`

### Parameters

| Name         | In     | Type                                                                                                                                                                                                     | Required | Description  |
| ------------ | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ |
| workspace_id | path   | string                                                                                                                                                                                                   | true     | workspace id |
| x-auth-token | header | string                                                                                                                                                                                                   | true     | access token |
| body         | body   | [media.UploadCallbackInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#schemamedia.uploadcallbackinput) | true     |              |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                     |
| ------ | ------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [media.UploadCallbackOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#schemamedia.uploadcallbackoutput) |

> Example responses

```
{
    "code":0,
    "message":"success",
       "data":{
      "object_key":"5asjwu24-2a18-4b4b-86f9-3a678da0bf4d/example.jpg"
    }
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#schemas)Schemas

## media.UploadCallbackInput

```
{
  "result": 0,
  "ext": {
    "file_group_id": "string",
    "drone_model_key": "string",
    "is_original": true,
    "payload_model_key": "string",
    "tinny_fingerprint": "string",
    "sn": "string"
  },
  "fingerprint": "string",
  "metadata": {
    "absolute_altitude": 0,
    "created_time": "string",
    "gimbal_yaw_degree": 0,
    "relative_altitude": 0,
    "shoot_position": {
      "lat": 0,
      "lng": 0
    }
  },
  "name": "string",
  "object_key": "string",
  "path": "string",
  "sub_file_type": 0
}
```

*Properties*

| Name          | Type                                                                                                                                                                                 | Required | Restrictions | Description                                                                              |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | ---------------------------------------------------------------------------------------- |
| result        | int                                                                                                                                                                                  | true     | none         | Whether the file is successfully uploaded to the storage bucket. Non-zero means failure. |
| ext           | [media.MediaFile](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#schemamedia.mediafile) | false    | none         | extended attributes for file associations                                                |
| fingerprint   | string                                                                                                                                                                               | false    | none         | file fingerprint                                                                         |
| metadata      | [media.MetaData](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#schemamedia.metadata)   | false    | none         | media metadata                                                                           |
| name          | string                                                                                                                                                                               | true     | none         | filename                                                                                 |
| object_key    | string                                                                                                                                                                               | true     | none         | the key of the file in the object storage bucket                                         |
| path          | string                                                                                                                                                                               | false    | none         | the business path of the file                                                            |
| sub_file_type | integer                                                                                                                                                                              | false    | none         | valid when the file is a picture<br>* 0 - normal picture<br>* 1 - panorama               |

## media.MediaFile

```
{
  "file_group_id": "string",
  "drone_model_key": "string",
  "is_original": true,
  "payload_model_key": "string",
  "tinny_fingerprint": "string",
  "sn": "string"
}
```

*Properties*

| Name              | Type    | Required | Restrictions | Description                   |
| ----------------- | ------- | -------- | ------------ | ----------------------------- |
| file_group_id     | string  | false    | none         | file group id                 |
| drone_model_key   | string  | false    | none         | device product enum           |
| is_original       | boolean | false    | none         | whether is the original image |
| payload_model_key | string  | false    | none         | payload product enum          |
| tinny_fingerprint | string  | false    | none         | tiny fingerprint              |
| sn                | string  | false    | none         | serial number                 |

## media.MetaData

```
{
  "absolute_altitude": 0,
  "created_time": "string",
  "gimbal_yaw_degree": 0,
  "relative_altitude": 0,
  "shoot_position": {
    "lat": 0,
    "lng": 0
  }
}
```

*Properties*

| Name              | Type    | Required | Restrictions | Description                     |
| ----------------- | ------- | -------- | ------------ | ------------------------------- |
| absolute_altitude | number  | false    | none         | absolute height                 |
| created_time      | string  | false    | none         | media created time              |
| gimbal_yaw_degree | number  | false    | none         | gimbal yaw degree               |
| relative_altitude | integer | false    | none         | relative height                 |
| shoot_position    | object  | false    | none         | capturing position              |
| » lat             | integer | false    | none         | latitude of capturing location  |
| » lng             | integer | false    | none         | longitude of capturing location |

## media.UploadCallbackOutput

```
{
  "code": 0,
  "data": {
    "object_key": "string"
  },
  "message": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                                                                                               | Required | Restrictions | Description |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ------------ | ----------- |
| code    | integer                                                                                                                                                                                                            | false    | none         | error code  |
| data    | [media.UploadCallbackOutputData](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/mediafile-upload-result-report.html#schemamedia.uploadcallbackoutputdata) | false    | none         | none        |
| message | string                                                                                                                                                                                                             | false    | none         | description |

## media.UploadCallbackOutputData

```
{
  "object_key": "string"
}
```

*Properties*

| Name       | Type   | Required | Restrictions | Description                                      |
| ---------- | ------ | -------- | ------------ | ------------------------------------------------ |
| object_key | string | false    | none         | the key of the file in the object storage bucket |
