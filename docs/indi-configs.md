# INDI Configs

LumiSky needs to set gain (and preferably offset) but INDI does not provide a standardized way to do this.
The property names vary among vendor devices so you must tell LumiSky how to set gain, offset, and other parameters.

# Gain and Offset

LumiSky knows the mappings for popular manufacturers like ZWO, QHY, etc. but for other cameras you may need to provide custom mappings.

| Vendor       | Gain Property Name   | Offset Property Name  |
| :----------- | :------------------: | :-------------------: |
| ZWO          | `CCD_CONTROLS:Gain`  | `CCD_CONTROLS:Offset` |
| QHY          | `CCD_GAIN:Gain`      | `CCD_OFFSET:Offset`   |
| ToupTek      | `CCD_CONTROLS:Gain`  | `CCD_OFFSET:Offset`   |
| PlayerOne    | `CCD_CONTROLS:Gain`  | `CCD_CONTROLS:Offset` |
| Svbony       | `CCD_CONTROLS:Gain`  | `CCD_CONTROLS:Offset` |
| Raspi Camera | `CCD_GAIN:Gain`      | (Not Supported)       |

# Custom Properties

You can set vendor specific INDI properties as needed.
These properties may be necessary for your camera.
You can set image format, USB bandwidth, high gain mode, etc.
The properties you set will be used when taking any image.

The configuration format is json.
LumiSky recommends to run your json through an online [json linter](https://jsonformatter.org) to ensure the config syntax is correct.

## ZWO

Turn on 16-bit images, set usb bandwidth.

``` json
[
  {
    "Property": "CCD_VIDEO_FORMAT",
    "Field": "ASI_IMG_RAW16",
    "Value": true
  },
  {
    "Property": "CCD_CONTROLS",
    "Field": "BandWidth",
    "Value": 40
  }
]
```

## ToupTek

Turn on raw bayered images, turn on high gain mode.

```json
[
  {
    "Property": "CCD_CAPTURE_FORMAT",
    "Field": "INDI_RAW",
    "Value": true
  },
  {
    "Property": "TC_CONVERSION_GAIN",
    "Field": "GAIN_HIGH",
    "Value": true
  }
]
```

## PlayerOne

Turn on 16-bit images, set usb bandwidth.

```json
[
  {
    "Property": "CCD_VIDEO_FORMAT",
    "Field": "POA_RAW16",
    "Value": true
  },
  {
    "Property": "CCD_CONTROLS",
    "Field": "USBBandWidthLimit",
    "Value": 35
  }
]
```

## Svbony

Turn on 16-bit images.

```json
[
  {
    "Property": "CCD_VIDEO_FORMAT",
    "Field": "SVB_IMG_RAW16",
    "Value": true
  }
]
```

## Raspi Camera

Turn off auto white balance.

```json
[
  {
    "Property": "AwbMode",
    "Field": "Awb Mode",
    "Value": "custom"
  },
  {
    "Property": "Adjustments",
    "Field": "AwbRed",
    "Value": 1
  },
  {
    "Property": "Adjustments",
    "Field": "AwbBlue",
    "Value": 1
  }
]
```