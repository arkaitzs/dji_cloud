# Obtain Temporary Credential

2025-03-19

No Rating

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/20.obtain-temporary-credential.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-temporary-credential.html#get-sts-token)Get STS Token

`POST /storage/api/v1/workspaces/{workspace_id}/sts`

### Parameters

| Name         | In     | Type   | Required | Description  |
| ------------ | ------ | ------ | -------- | ------------ |
| workspace_id | path   | string | true     | workspace id |
| x-auth-token | header | string | true     | access token |

### Responses

| Status | Meaning                                                 | Description | Schema                                                                                                                                                                                         |
| ------ | ------------------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | [storage.GetStsOutput](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-temporary-credential.html#schemastorage.getstsoutput) |

> Example responses

```
{
    "code":0,
    "data":{
        "bucket":"string",
        "credentials":{
            "access_key_id":"STS.NUBdKtVadL1U8aBJ2TH6PWoYo",
            "access_key_secret":"9NG2P2yJaUrck576CkdRoRbchKssJiZygi5D93CBsduY",
            "expire":3600,
              "security_token":"CAIS8AN1q6Ft5B2yfSjIr5b3L/HAu75F+/O+OkfzrjIBRLl8uKryjTz2IHhOenBhB+Actfk+lWhV6v0Tlrt+UMQcHQnKbM99q49L9hmobIeZWV4pagxD2vOfAmG2J0PRH6WwCryLq7q/F96pb1fb7FgRpZLxaTSlWXG8LJSNkuQJR98LXw6+H1UkadBNPVkg0sJ4U0HcLvGwKBXnr3PNBU5zwGpGhHh49L60z7+9iDXXh0aozfQO9cajYMqkfYxiPZNyFsyp2/Z/eaeEzCNL918X/fl43aAY83Kdt4rNRgVbvx/DY7Tao5g0JVEmNqQzQ6RK8PG714+D046+voDzzAk3fIMxei/DRYem7dLZEeeyTLgQfqr6PHK/q7LoMYLu4Sclem48PgFHcMY6UCUSbyYhUTbHMKSq1UnXawO4Mci/3boxzIB+wieWn6aDLEPdRK6Cg2RKeM05flsoMAIRxhaiEM09bxZNdVxDBrWYN+d0dwsMkbnlswzCJFQCqXFeufLsZ/TL/fpHMNi4HLA+iNpCPcQa6zd6Fg+rEunw1n15LjI1Hexkt4D2IoK65bO/x+GeXPXLEPhvuC8BKWqP9nvTGSkLcHygvoB/MguCjt/N1+nM4dZuEQ8jo8tDChuMftsos1F9/+6o6BCe4DNU548fW6tTGoABfvC8lAYwapu2ryxHRLeBodm278eCTa57hXytE/f/l9neR9Zg9tLoIJzFOdjs2gLfVc+BhjQ0GkZDP9ie332XnhH5nOugICpYlv5++p2Ap6WZIKTVEkFetdVKjkxal2zhXoCN9Aq4YeLn5bfQiTHrA3pjjhuE7sMSFsMVdxVvftI="
        },
        "endpoint":"https://oss-cn-hangzhou.aliyuncs.com",
        "object_key_prefix":"5a6f9d4b-2a38-4b4b-86f9-3a678da0bf4a",
        "provider":"ali"
    },
    "message":"success"
}
```

# [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-temporary-credential.html#schemas)Schemas

## storage.GetStsOutput

```
{
    "code":0,
    "message":"string",
    "data":{
        "bucket":"string",
        "credentials":{
            "access_key_id":"string",
            "access_key_secret":"string",
            "expire":0,
            "security_token":"string"
        },
        "endpoint":"string",
        "object_key_prefix":"string",
        "provider":"ali",
        "region":"string"
    }
}
```

*Properties*

| Name                | Type                                                                                                                                                                                         | Required | Restrictions | Description                                                    |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------ | -------------------------------------------------------------- |
| code                | string                                                                                                                                                                                       | false    | none         | error code                                                     |
| message             | string                                                                                                                                                                                       | false    | none         | description                                                    |
| data                | string                                                                                                                                                                                       | false    | none         |                                                                |
| » bucket            | string                                                                                                                                                                                       | false    | none         | bucket name                                                    |
| » credentials       | [storage.Credentials](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/obtain-temporary-credential.html#schemastorage.credentials) |          | none         | credentials info                                               |
| » endpoint          | string                                                                                                                                                                                       | false    | none         | access domain name for external services                       |
| » object_key_prefix | string                                                                                                                                                                                       | false    | none         | The prefix of the key of the file in the object storage bucket |
| » provider          | string                                                                                                                                                                                       | false    | none         | enum:<br>* ali<br>* aws                                        |
| » region            | string                                                                                                                                                                                       | false    | none         | The region where the data center is located                    |

## storage.Credentials

```
{
  "access_key_id": "string",
  "access_key_secret": "string",
  "expire": 0,
  "security_token": "string"
}
```

*Properties*

| Name              | Type    | Required | Restrictions | Description                |
| ----------------- | ------- | -------- | ------------ | -------------------------- |
| access_key_id     | string  | false    | none         | access key id              |
| access_key_secret | string  | false    | none         | access key                 |
| expire            | integer | false    | none         | access key expiration time |
| security_token    | string  | false    | none         | session token              |
