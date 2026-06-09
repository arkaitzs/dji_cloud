# Live Stream

2025-07-02

4.6 Ratings

5 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/30.feature-set/10.pilot-feature-set/30.pilot-livestream.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#overview)Overview

The live streaming mainly sends the camera payload of the aircraft and the video stream of the DJI Dock to the tripartite cloud platform for broadcasting, and users can conveniently broadcast live on the remote web page.

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#supported-live-streaming-types)Supported Live Streaming Types

| Type        | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Agora       | The DJI public cloud platform is also based on the "Interactive Live Streaming Standard" function of Agora. The overall live broadcast delay is relatively low and the effect is good.<br>For the third-party cloud privatization deployment, Agora also provides a hybrid cloud deployment model. The data is in the customer's private server, and then a link is opened through the air gap to the Agora's public cloud. This link channel is mainly used to upgrade and operate and maintain the privatized deployed servers.                                                                                                                                                                                                                                                                                                                            |
| RTMP        | Real-Time Messaging Protocol (RTMP) is a communication protocol for streaming audio, video and data over the Internet. This protocol is based on TCP and is a protocol family that includes the RTMP and RTMPT, RTMPS, RTMPE and many other variants.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| RTSP        | RTSP (Real Time Streaming Protocol) is an application layer protocol in the TCP/IP protocol architecture that defines how one-to-many applications can efficiently deliver multimedia data over IP networks. RTSP is on top of RTP and RTCP in architecture, and it uses TCP or UDP to complete data transmission. Compared with RTSP, HTTP transmits HTML, while RTSP transmits multimedia data.                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| GB28181     | GB/T 28181-2016 is a transmission control standard for the access of security video equipment to platforms in Mainland China. For some servers which already have 28181 downlink gateway, the data rate of DJI enterprise devices can be pushed to the server directly through this protocol.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| WebRTC/WHIP | WebRTC [(Web Real-Time Communication)](https://docs.dolby.io/streaming-apis/docs/webrtc-whip) is a technology that enables real-time communication of video and audio streams. It provides near real-time audio and video streams to ensure a smooth user experience. This technology is widely used in scenarios requiring high real-time communication, such as online meetings, online education, and telemedicine.<br>WHIP [(WebRTC-HTTP Ingestion Protocol)](https://millicast.medium.com/whip-the-magic-bullet-for-webrtc-media-ingest-57c2b98fb285)is an HTTP-based protocol designed to provide a standardized signaling protocol between WebRTC producers and streaming media servers, facilitating the delivery of WebRTC streams into streaming media servers. It allows WebRTC-based content to be delivered to streaming media servers or CDNs. |

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#framework-for-live-streaming)Framework for Live Streaming

![image-20220320180829159](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/0acd5856-2052-412e-9cf7-1c9622ce16bc.png)

As shown above, the aircraft flight platform is not directly connected to the tripartite cloud platform, it needs to be forwarded through the remote control or the DJI Dock. The communication between the remote control and the DJI Dock and the aircraft is still using DJI private AirLink.

The tripartite cloud platform requires pre-deployment of MQTT and streaming media server. DJI's streaming protocol supports RTMP, RTSP, GB28181 and Agora. MQTT is mainly used for message communication, configuration information setting and reading.

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#pilot-interactive-timing-diagram)Pilot Interactive Timing Diagram

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#load-live-streaming-module)Load Live Streaming Module

DJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web PageThe live capability is included in the topic andneeds to be resolved by the server aftersubscription.Resolve the live_capacity structure in thestate topic, and save it in the databaseLoading live streaming moduleJSBridge:platformLoadComponentNote: You need to load live streaming modulethrough JSBridge interface in advance to uselive streaming function.Subscribe "thing/product/{gateway_sn}/state" topicSubscribe "thing/product/{gateway_sn}/osd" topicSubscribe "thing/product/{gateway_sn}/services_reply" topicSubscribe "thing/product/{gateway_sn}/services" topicPublish "thing/product/{gateway_sn}/state" topicPublish state topicDJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web Page

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#server-side-call-protocol-to-start-live-stream)Server Side call protocol to start live stream

DJI Pilot 2Pilot PreviewMQTT GatewayCloud ServerThird-party Web PageQuery the local database fordevice live capabilityParse and verify services topicThe checks include whether the camera is decoding,whether in flight control interface and so onSend a request to query the device live capabilityReturn live capability dataClick to start live streamingSending services topics of method="live_start_push"Publish services topicPublish "service_reply" topicPublish service_reply topicPublish osd topic that contains the live_status fieldPublish osd topicDJI Pilot 2Pilot PreviewMQTT GatewayCloud ServerThird-party Web Page

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#verification-succeeded)Verification Succeeded

DJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web PageTurn on the encoderPushing streams to live streaming serversReport opening successthrough service_reply topicPublish that live streamingwas successfully openedStart pulling the streamingDJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web Page

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#verification-failed)Verification Failed

DJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web PagePublish the failure and the reason for the failure of startthe live streaming through the service_reply topicPublish the failure messageDJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web Page

### [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#app-side-call-jsbridge-to-start-live-stream)App Side call JSBridge to start live stream

DJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web PageUser chooses to start or stopa live streaming from the flightcontrol screenGo to the DJI Pilot 2 webview page and pull thelive streaming configuration parameters in theserver-sideReturn live streaming parametersSet manual live streaming parameters, and initiatelive streaming immediately after the first settingJSBridge:liveshareSetConfigPush streamingPull the streaming and playDJI Pilot 2Pilot WebviewMQTT GatewayCloud ServerThird-party Web Page

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-livestream.html#detailed-api-realization)Detailed API Realization

- [JSBridge](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/jsbridge.html)
  
  - Load DJI Pilot 2 Live Module `window.djiBridge.platformLoadComponent(String name, String param)`  
    Before using the live streaming function, you need to pre-load Pilot2's live streaming module via JSBridge in the Webview, developers can consider adding the interface to load the live module directly during the up/down login stage.
  
  - App Side call JSBridge to start livestreaming  
    For scenarios that do not require live viewing in the background, but need to turn on live streaming when the user is using it and send the code stream back to the server for archival analysis. The interface can be combined to allow users to manually trigger the live streaming function in Pilot2, with the following detailed steps.
    
    1. After logging into the three-party cloud platform in the Webview of Pilot2, you need to request a live streaming server address parameter from the server, which is configured differently by each three-party cloud platform, or you can write it directly in the front-end code.
    2. Send the live streaming parameters to DJI Pilot 2 for setting through the JSBridge interface.
    3. After DJI Pilot 2 receives the live broadcast configuration, it immediately initiates a live stream push, and users can enter the flight interface to view live information, stop the live broadcast, restart the live broadcast, and other operations.
    
    > **Note:** In the manual live streaming mode, the streaming image is always the main image stream of DJI Pilot 2. When DJI Pilot 2 switches the camera image, the streaming image will also change accordingly.

- [Live Stream (MQTT)](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-plus/live.html)
  
  - Live Capacity  
    The field live_capacity is placed in the object model of the gateway device and is only pushed when there is a state change on the device side.
  
  - Start Live Streaming  
    The server sends the command `method=live_start_push` to the device via MQTT, which uses the service method of the thing model to interact.
  
  - Stop Live Streaming
  
  - Set Live Streaming Quality
