# LumiSky

LumiSky is an astronomy allsky camera server for capturing images and generating timelapse videos.

LumiSky has been deployed at [SDSO](https://sdso.space/) since August 2024.

## Features

- Supported cameras
  - [INDI](https://www.indilib.org/) cameras
  - [Raspberry Pi](https://www.raspberrypi.com/documentation/accessories/camera.html) cameras 
- Dusk/dawn transition auto exposure
- Automatic timelapse generation
- Panorama images and timelapse video
- 3D panorama image and video player
- Host a publicly accessible web site via cloudflare workers
- Midtone autostretch
- Auto contrast enhance (S Curve)
- Accurate white balance
- Hot pixel correction
- Circle mask
- Customizable overlay
  - Cardinal directions
  - Timestamp
  - Latitude
  - Longitude
  - Elevation
  - Exposure
  - Gain
  - Sun altitude
  - Custom text
- FTP export
- Automatic cleanup of old data
- Import/export settings
- Focus mode

## Requirements

- OS
  - Debian 12 (bookworm)
  - Windows 10/11
- CPU
  - x86_64
  - ARM64 (linux only)
- RAM
  - 2GB RAM recommended, 1GB minimum
  - More RAM is required depending on camera resolution
- Storage
  - Enough storage for images, panoramas, timelapse videos
- Camera
  - Any camera supported by [INDI](https://www.indilib.org/)
- INDI
  - v2.0.7 and up

LumiSky can be run as a:

- Docker container (recommended)
- LXC
- Virtual machine
- Native application

## Screenshots

### Dashboard

![Dashboard](/docs/_media/lumisky-dashboard.jpg "Dashboard")

### Gallery

![Gallery](/docs/_media/lumisky-gallery.jpg "Gallery")

### 3D Panorama

<video src="https://github.com/user-attachments/assets/94e1b260-5859-486d-a0e2-37c0faa8521c" width="300"></video>

### Settings

![Settings](/docs/_media/lumisky-settings.jpg "Settings")
