# Obtain Wayline File Download Address

2025-03-19

4 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/60.api-reference/10.pilot-to-cloud/10.https/20.waypoint-management/30.get-waypointfile-download-location.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/waypoint-management/get-waypointfile-download-location.html#get-waypoints-download-url-link)Get Waypoints Download URL Link

`GET /wayline/api/v1/workspaces/{workspace_id}/waylines/{id}/url`

### Parameters

| Name         | In     | Type   | Required | Description       |
| ------------ | ------ | ------ | -------- | ----------------- |
| workspace_id | path   | string | true     | workspace id      |
| id           | path   | string | true     | waypoints file id |
| x-auth-token | header | string | true     | access token      |

### Responses

| Status | Meaning                                                 | Description | Schema |
| ------ | ------------------------------------------------------- | ----------- | ------ |
| 200    | [OK](https://tools.ietf.org/html/rfc7231#section-6.3.1) | OK          | /      |
