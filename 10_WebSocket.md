# WebSocket

2025-03-19

3.4 Ratings

5 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/10.overview/40.basic-concept/50.websocket.md) 

For some features, Websocket communication is required, so DJI Pilot 2 and the server need to establish a WebSocket channel. When DJI Pilot 2 logs in and goes online through Webview, the server needs to respond to the URL address of the WebSocket to DJI Pilot 2, and then DJI Pilot 2 will make a WebSocket connection request.

The specific general message push interface protocol format is as follows:

```
url: xxxx.xx.xxx

body:
{
    "biz_code": "device_topo_update",
    "version":"1.0"
    "timestamp": 146052438362,
    "data":{
        "operator": "zhangsan", //It is recommended to use callsign directly
         // The specific content is adjusted with biz_code or sub_biz_code
    }
}
```
