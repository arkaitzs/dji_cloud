# Obtain Exist File Tiny Fingerprint

2025-03-19

4 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/30.media-management/20.obtain-exited-tiny-fingerprint.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/obtain-exited-tiny-fingerprint.html#get-file-tiny-fingerprint)Get File Tiny Fingerprint

`POST /media/api/v1/workspaces/{workspace_id}/files/tiny-fingerprints`

### Parameters

| Name              | In     | Type          | Required | Description                 |
| ----------------- | ------ | ------------- | -------- | --------------------------- |
| tiny_fingerprints | body   | array[string] | true     | tiny fingerprint collection |
| workspace_id      | path   | string        | true     | workspace id                |
| x-auth-token      | header | string        | true     | access token                |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                                               |
| ------ | ------------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [media.GetTinyFingerprintsOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/obtain-exited-tiny-fingerprint.html#schemamedia.gettinyfingerprintsoutput) |

> Example responses

```
{
    "code":0,
    "message":"success",
    "data":{
        "tiny_fingerprints":[
            "5aec4c6e78052bf38fab901bcd1a2319_2021_12_8_22_13_10" 
        ]
    }
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/media-management/obtain-exited-tiny-fingerprint.html#schemas)Schemas

## media.GetTinyFingerprintsOutput

```
{
    "code":0,
    "message":"success",
    "data":{
        "tiny_fingerprints":[
            "string"
        ]
    }
}
```

*Properties*

| Name                | Type     | Required | Restrictions | Description                          |
| ------------------- | -------- | -------- | ------------ | ------------------------------------ |
| code                | string   | false    | none         | error code                           |
| message             | string   | false    | none         | description                          |
| data                | string   | false    | none         |                                      |
| » tiny_fingerprints | [string] | false    | none         | existing tiny fingerprint collection |
