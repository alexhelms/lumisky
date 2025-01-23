# Initial Setup

## Image Statistics

It is recommended to get some image statistics that are specific for your camera.
These statistics ensure accurate white balance and auto exposure.

At this time it is easier to collect this data manually outside of LumiSky.

?> A future version of LumiSky will be capable of manually taking images to assist with this step.

1. Open your favorite acquisition software like SharpCap, etc.
2. Connect to the camera.
3. Cover the sensor/lens.
4. Set the offset to the desired value.
5. Take a 0.3s image at your daytime gain and save the image as a FITS.
6. Take a 0.3s image at your nighttime gain and save the image as a FITS.
7. Debayer each bias image and note the median value in 16-bit ADU for the R, G, and B channels.

## Settings

### Data

LumiSky stores application data like logs, settings, and the database in the following location:

- Linux
  - `$HOME/.lumisky`
- Windows
  - `%USERPROFILE%\.lumisky`

The application data path can be overridden with the `LUMISKY_PATH` environment variable.

By default, image and video data is stored at `LUMISKY_PATH/data`. You can change this location in `Settings > Data`.

### Capture

Set the capture interval and max exposure in `Settings > Capture`.

Total capture job time should not exceed the capture interval or the next image will be late.
Capture job time consists of exposure time and INDI overhead.
The overhead is different on each setup but is typically 1-3 seconds.

### Camera

Select the typ of camera you are using:

- INDI
- Raspi Web
- Raspi Native
  - Only available if running LumiSky natively on a raspberry pi without docker.

This guide assumes you are using INDI.

Set the INDI hostname, port, and camera name in `Settings > INDI`.

LumiSky will try to connect to the INDI server and get the list of devices for you to select.

Next, select your device and camera manufacturer.

#### Exposure

Set the following settings to the same values used when your collected image statistics.
- Offset
- Daytime
  - Gain
  - Electron Gain
  - Red bias in ADU
  - Green bias in ADU
  - Blue bias in ADU
- Nighttime
  - Gain
  - Electron Gain
  - Red bias in ADU
  - Green bias in ADU
  - Blue bias in ADU

### Location

Set your allsky's location in `Settings > Location`.

This is important for calculating sunrise and sunset times.
