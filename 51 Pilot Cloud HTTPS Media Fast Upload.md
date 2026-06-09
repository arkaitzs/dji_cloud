# Media Fast Upload

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/30.media-management/10.fast-upload.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/fast-upload.html#media-quick-upload)Media Quick Upload

`POST /media/api/v1/workspaces/{workspace_id}/fast-upload`

### Parameters

| Name         | In     | Type                                                                                                                                                                          | Required | Description  |
| ------------ | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ |
| workspace_id | path   | string                                                                                                                                                                        | true     | workspace id |
| x-auth-token | header | string                                                                                                                                                                        | true     | access token |
| body         | body   | [media.FastUploadInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/fast-upload.html#schemamedia.fastuploadinput) | true     | body         |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                         |
| ------ | ------------------------------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [media.FastUploadOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/fast-upload.html#tocS_media.FastUploadOutput) |

> Example responses

```
{
    "code":0,
    "message":"success",
       "data":{}
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/fast-upload.html#schemas)Schemas

## media.FastUploadInput

```
{
  "ext": {
    "drone_model_key": "string",
    "is_original": true,
    "payload_model_key": "string",
    "tinny_fingerprint": "string",
    "sn": "string"
  },
  "fingerprint": "string",
  "name": "string",
  "path": "string"
}
```

*Properties*

| Name        | Type                                                                                                                                                              | Required | Restrictions | Description                               |
| ----------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------------------------------------- |
| ext         | [media.MediaFile](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/fast-upload.html#schemamedia.mediafile) | false    | none         | extended attributes for file associations |
| fingerprint | string                                                                                                                                                            | true     | none         | file fingerprint                          |
| name        | string                                                                                                                                                            | false    | none         | filename                                  |
| path        | string                                                                                                                                                            | false    | none         | the business path of the file             |

## media.MediaFile

```
{
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
| drone_model_key   | string  | false    | none         | device product enum           |
| is_original       | boolean | false    | none         | whether is the original image |
| payload_model_key | string  | false    | none         | payload product enum          |
| tinny_fingerprint | string  | false    | none         | tiny fingerprint              |
| sn                | string  | false    | none         | serial number                 |

## media.FastUploadOutput

```
{
    "code":0,
    "message":"success",
    "data":{}
}
```

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/fast-upload.html#properties)Properties

| Name    | Type   | Required | Restrictions | Description      |
| ------- | ------ | -------- | ------------ | ---------------- |
| code    | string | false    | none         | error code       |
| message | string | false    | none         | description      |
| data    | string | false    | none         | returned content |
