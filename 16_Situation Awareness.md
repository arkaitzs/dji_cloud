# Situation Awareness

2025-03-19

5 Ratings

1 customer rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/30.feature-set/10.pilot-feature-set/20.pilot-situation-awareness.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-situation-awareness.html#function-overview)Function Overview

TSA is a function that supports DJI Pilot 2 to display the aircraft/RC info on the map by using the device coordinate information which is sent by the Server end. Both web end and DJI Pilot 2 end will have all devices info under the same workspace. It can help the communication and info sharing between all devices/Pilot2/teammates.

For example, in the following figure,Pilot A,Pilot B, DOCK A, DOCK B, Human, and other devices have pushed their information to the server through API. Once the server end receives all information, it will summarize it and push it to different DJI Pilot 2 through WebSocket, thenPilot A andPilot B will have all info displayed in their APP.

![](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/833f63af-429b-4999-bd22-4d23d18f7c44.png)  

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-situation-awareness.html#interaction-sequence-diagram)Interaction Sequence Diagram

Pilot2 WebviewDJI Pilot 2Cloud ServerOthersSetup workspaceId, load api,ws,tsa moduleopt[device_online/offline/update]loop[status push]Log in to the 3rd party platformload api moduleload Ws moduleload TSA moduleWebsocket connectionconnectPilot2 online first time, request device list topologyresponsePush device remote sensing informationPush device remote sensing information at a fixed frequencyother devices online/offline/update statuspush device_online info through websocketprocess websocket inforequest device list topologyPilot2 WebviewDJI Pilot 2Cloud ServerOthers

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-situation-awareness.html#detailed-api-realization)Detailed API Realization

- [JSBridge](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/jsbridge.html)
  
  - Load DJI Pilot 2 TSA Module `window.djiBridge.platformLoadComponent(String name, String param)`  
    Before using the TSA function, developers need to set up the workspaceId, configure the Ws module and api module, and then load the DJI Pilot 2 map module. Also, developers can consider adding the loading interface of map module in log-in phase.

- [Situation Awareness (HTTPS)](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html)
  
  - Obtain Device Topology List  
    In the first connection, DJI Pilot 2 will send out an http request to obtain all devices list and topology list. On the server end, it needs to synchronize the device list to DJI Pilot 2. Also, if it receives an instruction of device online/offline/update from WebSocket, it needs the same interface to request the update of the device topology list.
  
  - Custom Icon  
    The device can display custom icons by using icon_urls. If the field is defined, it will be displayed in preference to the content of the field. If not, it is displayed by default by device_model.

```
"icon_urls":{      
                "normal_icon_url":"resource://Pilot2/drawable/tsa_aircraft_others_normal",    // Normal status icon
                "selected_icon_url":"resource://Pilot2/drawable/tsa_aircraft_others_pressed",   // Selected status icon
            }
```

App has some built-in icons.

```
url style:  resource://Pilot2/drawable/tsa_aircraft_others_normal
```

**Built-in Icon List**

| Icon                                                                                                                                                    | Icon_url                                        | Remark |
| ------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------- | ------ |
| ![GTScreenshot\_20220322\_172153.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/33e69a29-a625-4196-8955-535fab5a1ef2.png) | resource://Pilot2/drawable/tsa_car_select       |        |
| ![GTScreenshot\_20220322\_172131.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/c458f256-4011-4205-9d6c-4c6e19d7d319.png) | resource://Pilot2/drawable/tsa_car_normal       |        |
| ![GTScreenshot\_20220322\_172207.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/19ee7c1b-fc10-468a-bfc5-bf00b1d62cdc.png) | resource://Pilot2/drawable/tsa_person_select    |        |
| ![GTScreenshot\_20220322\_172226.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/256035a6-4fb0-4a1d-8c67-0cbbd218d593.png) | resource://Pilot2/drawable/tsa_person_normal    |        |
| ![GTScreenshot\_20220322\_172239.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/5feb755a-a9fb-4388-be4a-6731edc4c848.png) | resource://Pilot2/drawable/tsa_equipment_select |        |
| ![GTScreenshot\_20220322\_172248.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/811e604b-ed8a-49b0-aa02-48563e3833a4.png) | resource://Pilot2/drawable/tsa_equipment_normal |        |

Support online icon. Online Icons are downloaded and cached inside the App and displayed on the map at a fixed size (28dp).

```
url style: http://r56978dr7.hn-bkt.clouddn.com/tsa_equipment_normal.png
```

The icon in DJI Pilot 2 Map:

![](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/90d82dc5-dc7b-404a-8cec-208c4cf9d466.png)  

- [Situation Awareness (WebSocket)](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html) and [Remote Controller Properties](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-plus/properties.html)
  
  - Device Remote Sensing information Push  
    The Server end will push all devices' remote sensing information under the same workspace to DJI Pilot 2 and DJI Pilot 2 will update the status and position of the device based on the received data.
  
  - Device Online/Offline/Update Topology Status Push  
    When the Server end has received a request for any device online/offline/topology update, it will broadcast a device online/offline/topology update push notification to DJI Pilot 2. DJI Pilot 2 will trigger *"obtain device topology list"*# Situation Awareness
    
    2025-03-19
    
    5 Ratings
    
    1 customer rated
    
    [Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/30.feature-set/10.pilot-feature-set/20.pilot-situation-awareness.md) 
    
    ## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-situation-awareness.html#function-overview)Function Overview
    
    TSA is a function that supports DJI Pilot 2 to display the aircraft/RC info on the map by using the device coordinate information which is sent by the Server end. Both web end and DJI Pilot 2 end will have all devices info under the same workspace. It can help the communication and info sharing between all devices/Pilot2/teammates.
    
    For example, in the following figure,Pilot A,Pilot B, DOCK A, DOCK B, Human, and other devices have pushed their information to the server through API. Once the server end receives all information, it will summarize it and push it to different DJI Pilot 2 through WebSocket, thenPilot A andPilot B will have all info displayed in their APP.
    
    ![](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/833f63af-429b-4999-bd22-4d23d18f7c44.png)  
    
    ## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-situation-awareness.html#interaction-sequence-diagram)Interaction Sequence Diagram
    
    Pilot2 WebviewDJI Pilot 2Cloud ServerOthersSetup workspaceId, load api,ws,tsa moduleopt[device_online/offline/update]loop[status push]Log in to the 3rd party platformload api moduleload Ws moduleload TSA moduleWebsocket connectionconnectPilot2 online first time, request device list topologyresponsePush device remote sensing informationPush device remote sensing information at a fixed frequencyother devices online/offline/update statuspush device_online info through websocketprocess websocket inforequest device list topologyPilot2 WebviewDJI Pilot 2Cloud ServerOthers
    
    ## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/feature-set/pilot-feature-set/pilot-situation-awareness.html#detailed-api-realization)Detailed API Realization
    
    - [JSBridge](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/jsbridge.html)
      
      - Load DJI Pilot 2 TSA Module `window.djiBridge.platformLoadComponent(String name, String param)`  
        Before using the TSA function, developers need to set up the workspaceId, configure the Ws module and api module, and then load the DJI Pilot 2 map module. Also, developers can consider adding the loading interface of map module in log-in phase.
    
    - [Situation Awareness (HTTPS)](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/https/situation-awareness/obtain-device-topology-list.html)
      
      - Obtain Device Topology List  
        In the first connection, DJI Pilot 2 will send out an http request to obtain all devices list and topology list. On the server end, it needs to synchronize the device list to DJI Pilot 2. Also, if it receives an instruction of device online/offline/update from WebSocket, it needs the same interface to request the update of the device topology list.
      
      - Custom Icon  
        The device can display custom icons by using icon_urls. If the field is defined, it will be displayed in preference to the content of the field. If not, it is displayed by default by device_model.
    
    ```
    "icon_urls":{      
                    "normal_icon_url":"resource://Pilot2/drawable/tsa_aircraft_others_normal",    // Normal status icon
                    "selected_icon_url":"resource://Pilot2/drawable/tsa_aircraft_others_pressed",   // Selected status icon
                }
    ```
    
    App has some built-in icons.
    
    ```
    url style:  resource://Pilot2/drawable/tsa_aircraft_others_normal
    ```
    
    **Built-in Icon List**
    
    | Icon                                                                                                                                                    | Icon_url                                        | Remark |
    | ------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------- | ------ |
    | ![GTScreenshot\_20220322\_172153.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/33e69a29-a625-4196-8955-535fab5a1ef2.png) | resource://Pilot2/drawable/tsa_car_select       |        |
    | ![GTScreenshot\_20220322\_172131.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/c458f256-4011-4205-9d6c-4c6e19d7d319.png) | resource://Pilot2/drawable/tsa_car_normal       |        |
    | ![GTScreenshot\_20220322\_172207.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/19ee7c1b-fc10-468a-bfc5-bf00b1d62cdc.png) | resource://Pilot2/drawable/tsa_person_select    |        |
    | ![GTScreenshot\_20220322\_172226.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/256035a6-4fb0-4a1d-8c67-0cbbd218d593.png) | resource://Pilot2/drawable/tsa_person_normal    |        |
    | ![GTScreenshot\_20220322\_172239.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/5feb755a-a9fb-4388-be4a-6731edc4c848.png) | resource://Pilot2/drawable/tsa_equipment_select |        |
    | ![GTScreenshot\_20220322\_172248.png](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/811e604b-ed8a-49b0-aa02-48563e3833a4.png) | resource://Pilot2/drawable/tsa_equipment_normal |        |
    
    Support online icon. Online Icons are downloaded and cached inside the App and displayed on the map at a fixed size (28dp).
    
    ```
    url style: http://r56978dr7.hn-bkt.clouddn.com/tsa_equipment_normal.png
    ```
    
    The icon in DJI Pilot 2 Map:
    
    ![](https://terra-1-g.djicdn.com/84f990b0bbd145e6a3930de0c55d3b2b/admin/doc/90d82dc5-dc7b-404a-8cec-208c4cf9d466.png)  
    
    - [Situation Awareness (WebSocket)](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/websocket/situation-awareness/message-push.html) and [Remote Controller Properties](https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-plus/properties.html)
      
      - Device Remote Sensing information Push  
        The Server end will push all devices' remote sensing information under the same workspace to DJI Pilot 2 and DJI Pilot 2 will update the status and position of the device based on the received data.
      
      - Device Online/Offline/Update Topology Status Push  
        When the Server end has received a request for any device online/offline/topology update, it will broadcast a device online/offline/topology update push notification to DJI Pilot 2. DJI Pilot 2 will trigger *"obtain device topology list"*
