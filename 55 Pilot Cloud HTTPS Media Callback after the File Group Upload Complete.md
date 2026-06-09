# Callback after the File Group Upload Complete

2025-03-19

1 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/30.media-management/50.group-upload-callback.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/group-upload-callback.html#callback-after-the-file-group-upload-complete)Callback after the File Group Upload Complete

`POST /media/api/v1/workspaces/{workspace_id}/group-upload-callback`

### Parameters

| Name         | In     | Type                                                                                                                                                                                                            | Required | Description    |
| ------------ | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------- |
| workspace_id | path   | string                                                                                                                                                                                                          | true     | Workspace ID   |
| x-auth-token | header | string                                                                                                                                                                                                          | true     | Access Token   |
| body         | body   | [storage.FolderUploadCallbackInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/group-upload-callback.html#schemastorage.folderuploadcallbackinput) | true     | Body parameter |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                             |
| ------ | ------------------------------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [storage.FolderUploadCallbackOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/group-upload-callback.html#schema_storage.FolderUploadCallbackOutput) |

> Example responses

```
{
    "code":0,
       "data":{},
    "message": "success"
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/group-upload-callback.html#schemas)Schemas

## storage.MissionFinishCallBackInput

```
{
  "file_group_id": "xxx",
  "file_count": 0,
  "file_uploaded_count": 0
}
```

*Properties*

| Name                | Type   | Required | Restrictions | Description                                                                 |
| ------------------- | ------ | -------- | ------------ | --------------------------------------------------------------------------- |
| file_group_id       | string | false    | none         | File Group ID. The file group IDs created in one wayline task are the same. |
| file_count          | int    | true     | none         | Total media file number of file group.                                      |
| file_uploaded_count | int    | true     | none         | Total successfully uploded media file number of file group.                 |

## storage.FolderUploadCallbackOutput

```
{
  "code": 0,
  "data": {},
  "message": "string"
}
```

*Properties*

| Name    | Type                                                                                                                                                                                                      | Required | Restrictions | Description       |
| ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | ----------------- |
| code    | integer                                                                                                                                                                                                   | false    | none         | Error code        |
| data    | [storage.CreateFavoriteInput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/group-upload-callback.html#schemastorage.folderuploadcallbackinput) | false    | none         | None              |
| message | string                                                                                                                                                                                                    | false    | none         | Error description |
