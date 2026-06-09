# Product Supported

2026-03-19

3.2 Ratings

7 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/10.overview/30.product-support.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html#supported-model)Supported Model

> **Notes:**
> 
> - Cloud API does not support M200 V2 series, M200 series, M2E, P4R, M2EA and other old drones.
> - If you want to get more specific information about our drones, please go to [DJI Official website](https://www.dji.com/) and search for it. For example: [DJI Dock](https://www.dji.com/dock).

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html#introduction)Introduction

- Three attributes of `domain`, `type`, `sub_type` can determine a device (maybe drone, payload, remote controller and so on).
- Three attributes of `type`, `sub_type`, `gimbalindex` can determine a payload, on which drone and gimbal port it mounted.
  - 0, For M300 RTK, it is the left gimbal port under the drone if your sight follows the heading direction of the drone's nose. For other models, it is the main gimbal port.
  - 1, For M300 RTK, it is the right gimbal port under the drone if your sight follows the heading direction of the drone's nose.
  - 2, For M300 RTK, it is the gimbal port above the drone.
  - 7, It is the FPV camera.
  - Other values are reserved enumeration values.
- The domain represents a domain, as a namespace, temporarily divided into four types:
  - 0, Aircraft
  - 1, Payload
  - 2, Remote Controller
  - 3, DJI Dock
- The name is the common name for cloud platforms and SDK.

| Aircraft                                                                                                    | Gateway                                                                                         | Payload                      | Comment                                                                                                                                  |
| ----------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- | ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| [Matrice 4D/Matrice 4TD](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) | [DJI Dock 3](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html) | --                           | Can't support third-party payload now.                                                                                                   |
| [Matrice 3D/Matrice 3TD](https://enterprise.dji.com/dock-2)                                                 | [DJI Dock 2](https://enterprise.dji.com/dock-2)                                                 | --                           | Can't support third-party payload now.                                                                                                   |
| [Matrice 350 RTK](https://enterprise.dji.com/matrice-350-rtk)                                               | DJI RC Plus + DJI Pilot 2                                                                       | H20/H20T<br>H20N<br>H30/H30T | Can't support third-party payload now.<br>In the following documents, models are referred to simply as M350 RTK.                         |
| [Matrice 300 RTK](https://www.dji.com/matrice-300)                                                          | DJI RC Plus + DJI Pilot 2                                                                       | H20/H20T<br>H20N<br>H30/H30T | Can't support third-party payload now.<br>In the following documents, models are referred to simply as M300 RTK.                         |
| [Matrice 300 RTK](https://www.dji.com/matrice-300)                                                          | DJI Smart Controller Enterprise + DJI Pilot 2                                                   | H20/H20T<br>H20N<br>H30/H30T | Can't support third-party payload now.                                                                                                   |
| [Matrice 400](https://www.dji.com/matrice-400)                                                              | DJI RC Plus 2 + DJI Pilot 2                                                                     | H30/H30T                     | Can't support third-party payload now.                                                                                                   |
| [Matrice 30/Matrice 30T](https://www.dji.com/matrice-30)                                                    | DJI RC Plus + DJI Pilot 2                                                                       | --                           | Can't support third-party payload now.<br>In the following documents, models are referred to simply as M30/M30T.                         |
| [Matrice 30/Matrice 30T](https://www.dji.com/matrice-30)                                                    | [DJI Dock](https://www.dji.com/dock)                                                            | --                           | Can't support third-party payload now.                                                                                                   |
| [DJI Matrice 4E/DJI Matrice 4T](https://www.dji.com/matrice-4)                                              | DJI RC Plus 2 + DJI Pilot 2                                                                     | --                           | Can't support third-party payload now.<br>In the following documents, models are referred to simply as M4E/M4T.                          |
| [DJI Mavic 3 Enterprise Series](https://www.dji.com/mavic-3-enterprise)                                     | DJI RC Pro + DJI Pilot 2                                                                        | --                           | Can't support third-party payload now.<br>In the following documents<br>DJI Mavic 3 Enterprise Series are referred to simply as M3E/M3T. |

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html#enumeration-values-of-aircraft-rc-and-dock)Enumeration Values of Aircraft, RC and Dock

| name                                    | domain | type | sub_type | Description                                                             |
| --------------------------------------- | ------ | ---- | -------- | ----------------------------------------------------------------------- |
| Matrice 400                             | 0      | 103  | 0        | -                                                                       |
| Matrice 350 RTK                         | 0      | 89   | 0        | -                                                                       |
| Matrice 300 RTK                         | 0      | 60   | 0        | -                                                                       |
| Matrice 30                              | 0      | 67   | 0        | -                                                                       |
| Matrice 30T                             | 0      | 67   | 1        | -                                                                       |
| Mavic 3 Enterprise Series (M3E Camera)  | 0      | 77   | 0        | -                                                                       |
| Mavic 3 Enterprise Series (M3T Camera)  | 0      | 77   | 1        | -                                                                       |
| Mavic 3 Enterprise Series (M3TA Camera) | 0      | 77   | 3        | -                                                                       |
| Matrice 3D                              | 0      | 91   | 0        | -                                                                       |
| Matrice 3TD                             | 0      | 91   | 1        | -                                                                       |
| Matrice 4D                              | 0      | 100  | 0        | -                                                                       |
| Matrice 4TD                             | 0      | 100  | 1        | -                                                                       |
| DJI Matrice 4 Series（M4E Camera）        | 0      | 99   | 0        | -                                                                       |
| DJI Matrice 4 Series（M4T Camera）        | 0      | 99   | 1        | -                                                                       |
| DJI Smart Controller Enterprise         | 2      | 56   | 0        | Compatible with Matrice 300 RTK                                         |
| DJI RC Plus                             | 2      | 119  | 0        | Compatible with<br>Matrice 350 RTK<br>Matrice 300 RTK<br>Matrice 30/30T |
| DJI RC Plus 2                           | 2      | 174  | 0        | Compatible with<br>DJI Matrice 4 Series                                 |
| DJI RC Pro Enterprise                   | 2      | 144  | 0        | Compatible with <br>Mavic 3 Enterprise Series                           |
| DJI Dock                                | 3      | 1    | 0        | -                                                                       |
| DJI Dock 2                              | 3      | 2    | 0        | -                                                                       |
| DJI Dock 3                              | 3      | 3    | 0        | -                                                                       |

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/overview/product-support.html#enumeration-values-of-camera)Enumeration Values of Camera

<style>
</style>

|                               |        |                                                                                                                           |                                  |
| ----------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------- | -------------------------------- |
| name                          | domain | type-subtype-gimbalindex                                                                                                  | Description                      |
| Matrice 300 RTK  FPV          | 1      | 39-0-7                                                                                                                    | -                                |
| Matrice 350 RTK FPV           | 1      | 39-0-7                                                                                                                    | -                                |
| Matrice 30 FPV                | 1      | 39-0-7                                                                                                                    | -                                |
| Matrice 30T FPV               | 1      | 39-0-7                                                                                                                    | -                                |
| Matrice 400 FPV               | 1      | 39-0-7                                                                                                                    | -                                |
| Matrice 3D Vision Assist      | 1      | 176-0-0                                                                                                                   | -                                |
| Matrice 3TD Vision Assist     | 1      | 176-0-0                                                                                                                   | -                                |
| Matrice 4D Vision Assist      | 1      | 176-0-0                                                                                                                   | -                                |
| Matrice 4TD Vision Assist     | 1      | 176-0-0                                                                                                                   | -                                |
| Zenmuse Z30                   | 1      | The<br> position where the camera is mounted on the aircraft Portside (Main): 20-0-0<br> Starboard: 20-0-1 Upside: 20-0-2 | -                                |
| Zenmuse<br> XT2               | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 26-0-0<br> Starboard: 26-0-1 Upside: 26-0-2 | -                                |
| Zenmuse<br> XTS               | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 41-0-0<br> Starboard: 41-0-1 Upside: 41-0-2 | -                                |
| Zenmuse<br> H20               | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 42-0-0<br> Starboard: 42-0-1 Upside: 42-0-2 | -                                |
| Zenmuse<br> H20T              | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 43-0-0<br> Starboard: 43-0-1 Upside: 43-0-2 | -                                |
| Zenmuse<br> H20N              | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 61-0-0<br> Starboard: 61-0-1 Upside: 61-0-2 | -                                |
| Zenmuse<br> H30               | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 82-0-0<br> Starboard: 82-0-1 Upside: 82-0-2 | -                                |
| Zenmuse<br> H30T              | 1      | The position<br> where the camera is mounted on the aircraft Portside (Main): 83-0-0<br> Starboard: 83-0-1 Upside: 83-0-2 | -                                |
| Matrice<br> 30 Camera         | 1      | Main of<br> aircraft: 52-0-0                                                                                              | -                                |
| Matrice 30T Camera            | 1      | Main<br> of aircraft: 53-0-0                                                                                              | -                                |
| DJI Matrice 4E Camera         | 1      | Main<br> of aircraft: 88-0-0                                                                                              | -                                |
| DJI Matrice 4T Camera         | 1      | Main<br> of aircraft: 89-0-0                                                                                              | -                                |
| Mavic 3E Camera               | 1      | Main<br> of aircraft: 66-0-0                                                                                              | -                                |
| Mavic 3T Camera               | 1      | Main<br> of aircraft: 67-0-0                                                                                              | -                                |
| Mavic 3TA Camera              | 1      | Main<br> of aircraft: 129-0-0                                                                                             | -                                |
| Matrice 3D Camera             | 1      | Main<br> of aircraft: 80-0-0                                                                                              | -                                |
| Matrice 3TD Camera            | 1      | Main<br> of aircraft: 81-0-0                                                                                              | -                                |
| Matrice 4D Camera             | 1      | Main<br> of aircraft: 98-0-0                                                                                              | -                                |
| Matrice 4TD Camera            | 1      | Main<br> of aircraft: 99-0-0                                                                                              | -                                |
| DJI Dock inside camera        | 1      | 165-0-7                                                                                                                   | DJI<br> Dock camera_position: 1  |
| DJI<br> Dock 2 outside camera | 1      | 165-0-7                                                                                                                   | DJI Dock2<br> camera_position: 0 |
| DJI<br> Dock 2 outside camera | 1      | 165-0-7                                                                                                                   | DJI Dock2<br> camera_position: 1 |
| DJI<br> Dock 3 outside camera | 1      | 165-0-7                                                                                                                   | DJI Dock3<br> camera_position: 0 |
| DJI Dock<br> 3 outside camera | 1      | 165-0-7                                                                                                                   | DJI Dock3<br> camera_position: 1 |

> **Note:** `camera_position` is field of live_camera_change API.
