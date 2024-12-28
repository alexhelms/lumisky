using System.ComponentModel.DataAnnotations;

namespace LumiSky.Core.Imaging.Processing;

public enum OverlayVariable
{
    Timestamp,
    Latitude,
    Longitude,
    Elevation,
    Exposure,
    Gain,
    [Display(Description = "Sun Altitude")]
    SunAltitude,
    Text,
}

public enum TextAnchor
{
    [Display(Description = "Top Left")]
    TopLeft,
    [Display(Description = "Top Middle")]
    TopMiddle,
    [Display(Description = "Top Right")]
    TopRight,
    [Display(Description = "Middle Left")]
    MiddleLeft,
    [Display(Description = "Middle")]
    Middle,
    [Display(Description = "Middle Right")]
    MiddleRight,
    [Display(Description = "Bottom Left")]
    BaselineLeft,
    [Display(Description = "Bottom Middle")]
    BaselineMiddle,
    [Display(Description = "Bottom Right")]
    BaselineRight
}