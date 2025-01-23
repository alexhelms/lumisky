# Settings

?> You must click the `Save` button on each page.

## Import / Export

Export settings to make migration to new hardware easier.

Importing requires a restart of LumiSky for all settings to take affect.

## Data

### Paths

#### App Data Path

Absolute path to application data like logs, settings, database, etc.
Can be overridden with the `LUMISKY_PATH` environment variable

#### Image Data Path

Absolute path to image/video data. The default value is `LUMISKY_PATH/data`.

### Cleanup

A background cleanup job runs daily at 10am local time. You can manually run the job by pressing the `Run Now` button.

Cleanup can be toggled without affecting each data category's cleanup settings.

!> Data older than the specified age will be **permanently** deleted.

Any image or video tagged as a `favorite` **will not** be deleted by the cleanup job.
You can tag any image or video as a `favorite` in the gallery.

## Capture

### Auto Start <!-- {docsify-ignore} -->

When enabled, LumiSky will start capturing data when the application is started.

### Interval <!-- {docsify-ignore} -->

Capture job interval, in seconds.

The capture job is scheduled to run at this interval. The duration of the capture job
is the camera exposure time + saving the image + INDI overhead. If the total duration
of the capture job is longer than the capture interval the next capture job will be late
and the actual interval between images will be longer than desired. Reduce the
[Max Exposure](/settings?id=max-exposure) to get the capture job back on schedule.

### Max Exposure <!-- {docsify-ignore} -->

Max camera exposure time, in seconds.

## Camera

### Exposure

#### Binning

Camera binning from 1 to 4.

Default value is 1.

#### Offset

Camera offset, in arbitrary units used by the camera driver.

Default value is 0.

#### Gain

Camera gain to use during the daytime/nighttime, in arbitrary units used by the camera driver.

Default value is 0.

#### Electron Gain

Camera electron gain to use during the daytime/nighttime, in electrons per ADU.

Default value is 1.

Required for accurate auto exposure. This value comes from your camera manufacturer 
datasheet typically as a chart where the X axis is gain and the Y axis is e-/ADU.
Look at the chart for your daytime gain value and input the corresponding e-/ADU
gain as shown on the chart.

#### Bias

Per channel bias value in ADU.

Default value is 0.

Per channel bias is required for accurate white balance.

### Focal Length

Focal length, in millimeters.

The focal length is only used for image metadata.

## INDI

LumiSky connects to INDI at [hostname](/settings?id=hostname) and
[port](/settings?id=port). If LumiSky can connect to INDI it will show a list of devices.

If LumiSky can connect to INDI and your camera is in the list, press the `Use` button to
use that camera.

### Hostname

INDI hostname or IP address.

### Port

INDI port, default is 7624.

### Camera Name

INDI camera name. This must be the exact device name, e.g. `ZWO CCD ASI224MC`.

### Camera Manufacturer

You camera's manufacturer.

LumiSky tries to set this value automatically based on the [camera name](/settings?id=camera-name).

INDI devices use vendor specific property names for gain, offset, etc.
Knowing the camera manufacturer helps LumiSky determine the correct property names.

If needed, you can [customize](/indi-configs?id=indi-configs) the property mappings.

### Custom Config

Set a custom camera manufacturer to enable custom gain and offset mapping.

The mapping should be `PROPERTY_NAME:FIELD_NAME` and is case sensitive.

Unfortuntely, these mappings are not documented but are available in the indi device driver's [source code](https://github.com/indilib/indi-3rdparty).

See the [INDI Configs](/indi-configs?id=indi-configs) page for examples and additional information.

## Raspi

Configure a raspberry pi camera.

### Web

LumiSky can control a raspberry pi camera over a web API. This is a custom feature of LumiSky.

`Camera Url` should point to the base address of the LumiSky Rpicam server, e.g. `http://rpicam:8080`.

See the [raspberry pi camera](/raspi-camera) page for more information.

### Native

?> This section is only available if LumiSky is running as a native application on a raspberry pi without docker.

LumiSky will display any detected cameras along with its properties.

If your camera is not detected, check the flat flex cable for the camera.

If you are still having issues, check out [raspberry pi's documentation](https://www.raspberrypi.com/documentation/accessories/camera.html).

## Image

Select the image format you wish to save images.

Optionally, turn on `Keep Raw Images` to save the raw image as FITS.

### JPEG

Set the JPEG quality, 0 to 100.

Default value is 90.

0 is minimim quality, 100 is maximum quality.

### PNG

Set the PNG compression, 0 to 9.

Default value is 5.

0 is least compression, 9 is most compression.

### Image Geometry

The image is manipulated in the following order:
1. Rotate
2. Horizontal flip
3. Vertical flip

#### Image Rotation

Image rotation, in degrees, from -180 to 180.

Default value is 0.

A positive value rotates the image counter-clockwise.

#### Image Flip Horizontal

Flips the image horizontally.

#### Image Flip Vertical

Flips the image vertically.

### Panorama Geometry

When `Create Panoramas` is checked, Lumisky will create panorama images.

#### Diameter

Diameter of the fisheye circle in the image, in pixels.

Default value is 1000.

#### X Offset

X offset of the circle center, in pixels.

Change the panorama X offset if the fisheye circle is not in the center of the image.

#### Y Offset

Y offset of the circle center, in pixels.

Change the panorama Y offset if the fisheye circle is not in the center of the image.

#### Panorama Rotation

Panorama rotation, in degrees, from -180 to 180.

Default value is 0.

A positive value rotates the image counter-clockwise before transforming to a panorama image.

Change the panorama rotation if North is not on the left of the panorama image.

#### Panorama Flip Horizontal

Flips the panorama horizontally.

## Processing

### White Balance

On a partly cloud day adjust R, G, and B sliders until the clouds are white.

?> In very dark skies there can be large differences in color from night to night.
This is most likely airglow. Double check white balance during the daytime.

### Hot Pixel Correction

When enabled, hot pixels will be automatically corrected.

A hot pixel is corrected if its value is greater than [1 + threshold/100] times the max
of its NSEW neighbors. When corrected, the pixel is replaced with the average of its
four neighbors. The neighbors are always of the same color filter, respecting the CFA.

### Auto S Curve

When enabled, image contrast is enhanced by automatically applying a curves S-curve
according to the following equation:

$m = \small{\text{min}(\text{med}(\text{image}))}$

$\beta = \frac{-1}{\log_{2}(m)}$

$c =\small{\text{contrast}}$

$f(x) =
\begin{cases}
    \left[\frac{\left(2x^{\beta}\right)^{c}}{2}\right]^{\frac{1}{\beta}} & 0 \leq x < m\\ 
    \left[1-\frac{\left(2-2x^{\beta}\right)^{c}}{2}\right]^{\frac{1}{\beta}} & m \leq x \leq 1\\
\end{cases}$

### Circle Mask

When enabled, pixels outside of the circle will be set to black.

#### Diameter

Diameter of the circle, in pixels.

#### X Offset

X offset of the circle, in pixels.

#### Y Offset

Y offset of the circle, in pixels.

#### Blur Size

Blur size the circle's edge, in pixels.

### Overlay

When enabled, the text overlay will be drawn on the image.

Set the top, bottom, right, and left labels to the correct cardinal direction
for images.

Set the 0, 90, 180, and 270 degree azimuth labels for panoramas.

#### Text Size

Cardinal direction overlay text size, in pixels.

#### Outline Thickness

Cardinal direction overlay text outline thickness, in pixels.

#### Text Color

Cardinal direction overlay text color.

#### Outline Color

Cardinal direction overlay text outline color.

### Variable Overlay

Available variable overlays:

| Variable | Data Type | Text Format |
|----------|-----------|---------------|
| Timestamp | `DateTime` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings) |
| Latitude | `double` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) |
| Longitude | `double` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) |
| Elevation | `double` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) |
| Exposure | `double` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) |
| Gain | `double` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) |
| Sun Altitude | `double` | [Link](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) |
| Text | `string` | N/A |

Each variable overlay can have a:
- Text format
- X offset (from left)
- Y offset (from top)
- Font size, in pixels
- Text anchor
  - Top left
  - Top middle
  - Top right
  - Middle left
  - Middle
  - Middle right
  - Bottom left
  - Bottom middle
  - Bottom right
- Text color
- Stroke color
- Stroke width

## Location

#### Name

Name the location of your allsky camera.

#### Latitude

Latitude of your allsky camera, in degrees, -90 to 90.

#### Longitude

Longitude of your allsky camera, in degrees, -180 to 180, where east is positive.

#### Elevation

Elevation of your allsky camera, in meters.

## Timelapse

### Ffmpeg

#### Ffmpeg Path

Absolute path to ffmpeg executable.

#### Ffprobe Path

Absolute path to ffprobe executable.

### Timelapse

#### Daytime Timelapse

When enabled, daytime timelapses will be automatically generated.

#### Nighttime Timelapse

When enabled, nighttime timelapses will be automatically generated.

#### Codec

Available codecs:
- H264
- H265

Default codec is H264.

#### Frame Rate

Frame rate of the video.

Default value is 30.

#### Quality

CRF value for H264/H265 encoding.

A lower value leads to higher quality at the expense of a larger file size. 
For H264 consider a value of 25. 
For H265 consider a value of 28 to be about equivalent in quality.

#### Output Width

The pixel width of the output video.

Height is automatically calculated to maintain aspect ratio.
Reducing the pixel width can drastically reduce the size of the output video.
Set to 0 to disable scaling.

### Panorama Timelapse

#### Daytime Panorama Timelapse

When enabled, daytime panorama timelapses will be automatically generated.

#### Nighttime Panorama Timelapse

When enabled, nighttime panorama timelapses will be automatically generated.

#### Codec

Available codecs:
- H264
- H265

Default codec is H264.

#### Frame Rate

Frame rate of the video.

Default value is 30.

#### Quality

CRF value for H264/H265 encoding.

A lower value leads to higher quality at the expense of a larger file size. 
For H264 consider a value of 25. 
For H265 consider a value of 28 to be about equivalent in quality.

#### Output Width

The pixel width of the output video.

Height is automatically calculated to maintain aspect ratio.
Reducing the pixel width can drastically reduce the size of the output video.
Set to 0 to disable scaling.

## Export

When enabled, your data will be exported by all export methods that are enabled.

LumiSky can export:
- Raw images
- Images
- Panoramas
- Timelapse videos
- Panorama timelapse videos

### FTP

#### Host

Hostname or IP address of the FTP server.

#### Port

Port of the FTP server.

#### Username

Username for the FTP server.

#### Password

Password for the FTP server.

#### Remote Path

Remote path on the FTP server to upload data.

#### Validate Certificates

When enabled, only valid certificates will be accepted.

Default is false.

## Publish

### General

#### Enable Publishing

Toggle publishing without changing the specific publish settings.

#### Publish Image

Enable to publish the latest image.

#### Publish Panorama

Enable to publish the latest panorama.

#### Publish Night Timelapse

Enable to publish the most recent night timelapse when automatically created.

#### Publish Day Timelapse

Enable to publish the most recent day timelapse when automatically created.

### Display

#### Title

The title displaed on your published website.

#### Show Image

Enable to show the latest image.

#### Show Panorama

Enable to show the latest panorama and 3d viewer.

#### Show Night Timelapse

Enable to show the latest night timelapse video player.

#### Show Day Timelapse

Enable to show the latest day timelapse video player.

### Cloudflare Worker

#### Url

Your cloudflare worker public url.

#### API Key

Your worker's API key to allow LumiSky to publish data to your cloudflare worker.
