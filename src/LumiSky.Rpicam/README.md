# LumiSky.Rpicam

A web app for running `rpicam-still` via a web api.

Raspberry Pi cameras use `libcamera`, an arm64 library that interfaces with the camera hardware.
There is an INDI device driver for `libcamera` but it is not complete and generally unavailable
without manually compiling INDI. Additionally, it is only available for arm64 which complicates
the build process for LumiSky. Therefore, this project will run as a separate container service
and provide an api for invoking `rpicam-still` and retrieve image data for LumiSky.
