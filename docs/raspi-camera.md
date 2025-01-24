# Raspberry Pi Camera

LumiSky can use a raspberry pi camera in two ways: web and native.

First, follow [the official docs](https://www.raspberrypi.com/documentation/accessories/camera.html) to get your camera connected and working.

Try to use the official raspberry pi tools like `rpicam-hello --list-cameras` to verify the camera is connected before continuing.

## Web

LumiSky connects to a custom web app, LumiSky Rpicam, to control a raspberry pi camera over a web API.

LumiSky Rpicam runs in a container besides LumiSky on a raspberry pi.
It has access to the camera hardware and can take exposures and send the data back to LumiSky.

Using a raspberry pi camera with LumiSky requires:

- A raspberry pi
- Docker
- LumiSky running in a container
- LumiSky Rpicam running in in a container

Follow the [raspi installation guide](/installation?id=raspi-1) but ignore anything INDI and use the following docker compose:

[docker-compose.yml](/examples/raspicamera.docker-compose.yml ':include :type=code') 

Next, go to the LumiSky dashboard, `Settings > Raspi` and set the `Camera Url` to `http://rpicam:8080` and press `Test Connection`. Be sure to save!

Now complete the [initial setup](/initial-setup).

Finally, go to the Focus tab and start capturing images!

## Native

Lumisky can control a raspberry pi camera when Lumisky is running as a native application on a raspberry pi without docker.

First, ensure the raspberry pi camera is working outside of LumiSky.

Next, start LumiSky and go to `Settings > Camera > Camera Type` and select `Raspi Native`.
