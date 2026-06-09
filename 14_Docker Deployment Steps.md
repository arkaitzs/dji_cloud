# Docker Deployment Steps

2025-03-19

2.4 Ratings

9 customers rated

[Github Edit](https://github.com/dji-sdk/Cloud-API-Doc/blob/master/docs/en/20.quick-start/30.docker-deployment-steps.md) 

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#install-docker)Install Docker

- Installation tutorial: https://docs.docker.com/engine/install/ubuntu/

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#install-docker-compose)Install Docker Compose

- Installation tutorial: https://docs.docker.com/compose/install/

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#download-docker-source-code)Download Docker Source Code

- Click to download the [Source Code](https://terra-sz-hc1pro-cloudapi.oss-cn-shenzhen.aliyuncs.com/c0af9fe0d7eb4f35a8fe5b695e4d0b96/docker/cloud_api_sample_docker.zip)

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#unzip-the-file)Unzip the File

The directory structure after decompressing the cloud_api_sample_docker_1.0.0.zip file is as follows.

![image-20220321112952651](https://stag-terra-1-g.djicdn.com/7774da665e07453698314cc27c523096/admin/doc/195959b3-f8e1-4f3d-9d9b-d90ece297e15.png)

- data Store the user data when the demo service is running.

- docker-compose.yml The running configuration file for docker-compose.

- docs Store all types of documents, including API documents.

- source Store source code and source files for all types of mirrors.

- cloud_api_sample_docker_v1.0.0.tar Docker images for all environments.

- README.md

- update_backend.sh
  
  Build the back-end image file.

- update_front.sh
  
  Build the front-end image file.

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#load-images)Load Images

Use the **docker load** command to load this image file.

```
  sudo docker load < cloud_api_sample_docker_v1.0.0.tar
```

- Go to source/backend_service/sample/src/main/resources, modify the backend configuration file "application.yml", modify the MySQL configuration, MQTT configuration, Redis configuration and object storage server configuration in the configuration file.

- Go to source/nginx/front_page/src/api/http, modify the front-end configuration file "config.ts" and [enter the APP ID, APP Key and APP License applied for on the developer website.](https://developer.dji.com/en/user/apps/#all)

![](https://terra-1-g.djicdn.com/fee90c2e03e04e8da67ea6f56365fc76/SDK%20%E6%96%87%E6%A1%A3/CloudAPI/appinformation-en.jpeg)

> **Note:**
> 
> - If you are not using the live function, you only need to set the baseURL and websocketURL first. If you are using the amap, you also need to apply for an amapKey on the official website of amap.
> - rtmp parameter is streaming server address.
> - Except for these two configuration files, nothing else needs to be changed for the time being, just build and start the project to try.

- **Go to the directory of the update_front.sh file and build the front and back-end images.**
  
  ```
   # Build the front-end image file.
   ./update_front.sh
  
   # Build the back-end image file.
   ./update_backend.sh
  ```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#start-the-container)Start the Container

Go to the path of the docker-compose.yml file and use docker-compose to start all the images.

```
  sudo docker-compose up -d
```

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#login-in-pilot-2)Login in Pilot 2

1. Open pilot 2, go to the main page and click on Cloud Services to access it.
   
   ​ ![GTScreenshot\_20220321\_232931.png](https://terra-sz-hc1pro-cloudapi.oss-cn-shenzhen.aliyuncs.com/c0af9fe0d7eb4f35a8fe5b695e4d0b96/image/Screenshot_20220623-184322.png)

2. Click Open Platforms in the bottom right-hand corner.

![GTScreenshot\_20220321\_233150.png](https://terra-sz-hc1pro-cloudapi.oss-cn-shenzhen.aliyuncs.com/c0af9fe0d7eb4f35a8fe5b695e4d0b96/image/Screenshot_20220623-184704.png)

3. Enter the front-end access address (default: http://ip:8080/pilot-login), click on the **Connect** button in the top right-hand corner to access it.
   
   ​ ![GTScreenshot\_20220321\_233344.png](https://terra-sz-hc1pro-cloudapi.oss-cn-shenzhen.aliyuncs.com/c0af9fe0d7eb4f35a8fe5b695e4d0b96/image/Screenshot_20220623-184748.png)

4. Click the **Login** button to log in.
   
   username: pilot
   
   password: pilot123
   
   ​ ![GTScreenshot\_20220321\_233736.png](https://stag-terra-1-g.djicdn.com/7774da665e07453698314cc27c523096/admin/doc/76990178-c000-478b-ba45-2a57db8756fb.png)

5. If the main page shows Connected, you are successfully logged in, the remote control is connected to the emqx server and is pushing data. Now that the demo is up and running, you can click the back button on the remote control to return to the main page, as long as you don't click the **exit** button in the upper right corner, you will still be logged in.
   
   ​ ![GTScreenshot\_20220321\_233859.png](https://terra-sz-hc1pro-cloudapi.oss-cn-shenzhen.aliyuncs.com/c0af9fe0d7eb4f35a8fe5b695e4d0b96/image/Screenshot_20220623-184935.png)

6. You can already see the information on the workspace on the main page. As long as the font is dark black, it means that you are still logged in, and the data of the remote control and the drone will be continuously pushed. If you want to exit the workspace, you only need to click enter again and then click the **exit** button in the upper right corner to exit, and the remote control and the drone will no longer push data.
   
   ​ ![GTScreenshot\_20220321\_234607.png](https://terra-sz-hc1pro-cloudapi.oss-cn-shenzhen.aliyuncs.com/c0af9fe0d7eb4f35a8fe5b695e4d0b96/image/Screenshot_20220623-184955.png)

## [#](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/docker-deployment-steps.html#login-on-web-page)Login on Web Page

Default login page: http://ip:8080/project , `ip` should be changed to the real IP address that users are using.

username: adminPC

password: adminPC

 [Source Code Deployment Steps](https://developer.dji.com/doc/cloud-api-tutorial/en/quick-start/source-code-deployment-steps.html)
