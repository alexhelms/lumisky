using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.Collections;
using LumiSky.Core.Indi;

namespace LumiSky.Core.Profile;

public interface ICameraSettings : ISettings
{
    string CameraType { get; set; }

    string IndiDeviceName { get; set; }

    string IndiHostname { get; set; }

    int IndiPort { get; set; }

    string RaspiCameraUrl { get; set; }

    string CameraVendor { get; set; }

    string GainMapping { get; set; }

    string OffsetMapping { get; set; }

    string CustomProperties { get; set; }

    int Binning { get; set; }

    double FocalLength { get; set; }

    int Offset { get; set; }

    int DaytimeGain { get; set; }

    double DaytimeElectronGain { get; set; }

    double DaytimeBiasR { get; set; }

    double DaytimeBiasG { get; set; }

    double DaytimeBiasB { get; set; }

    int NighttimeGain { get; set; }

    double NighttimeElectronGain { get; set; }

    double NighttimeBiasR { get; set; }

    double NighttimeBiasG { get; set; }

    double NighttimeBiasB { get; set; }

    double TargetMedian { get; set; }
}

public sealed partial class CameraSettings : Settings, ICameraSettings
{
    protected override void Reset()
    {
        CameraType = "INDI";
        IndiDeviceName = string.Empty;
        IndiHostname = "localhost";
        IndiPort = 7624;
        RaspiCameraUrl = string.Empty;
        CameraVendor = IndiMappings.Vendor.ZWO;
        GainMapping = IndiMappings.GainMappings.First().Mapping;
        OffsetMapping = IndiMappings.OffsetMappings.First().Mapping;
        CustomProperties = string.Empty;
        Binning = 1;
        FocalLength = 10;
        Offset = 0;
        DaytimeGain = 0;
        DaytimeElectronGain = 1;
        DaytimeBiasR = 0;
        DaytimeBiasG = 0;
        DaytimeBiasB = 0;
        NighttimeGain = 0;
        NighttimeElectronGain = 1;
        NighttimeBiasR = 0;
        NighttimeBiasG = 0;
        NighttimeBiasB = 0;
        TargetMedian = 1000;
    }

    [ObservableProperty]
    public partial string CameraType { get; set; } = "INDI";

    [ObservableProperty]
    public partial string IndiDeviceName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IndiHostname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int IndiPort { get; set; } = 7624;

    [ObservableProperty]
    public partial string RaspiCameraUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CameraVendor { get; set; } = IndiMappings.Vendor.ZWO;

    [ObservableProperty]
    public partial string GainMapping { get; set; } = IndiMappings.GainMappings.First().Mapping;

    [ObservableProperty]
    public partial string OffsetMapping { get; set; } = IndiMappings.OffsetMappings.First().Mapping;

    [ObservableProperty]
    public partial string CustomProperties { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableDictionary<string, string> Extra { get; set; } = new();

    [ObservableProperty]
    public partial int Binning { get; set; }

    [ObservableProperty]
    public partial double FocalLength { get; set; }

    [ObservableProperty]
    public partial int Offset { get; set; }

    [ObservableProperty]
    public partial int DaytimeGain { get; set; }

    [ObservableProperty]
    public partial double DaytimeElectronGain { get; set; }

    [ObservableProperty]
    public partial double DaytimeBiasR { get; set; }

    [ObservableProperty]
    public partial double DaytimeBiasG { get; set; }

    [ObservableProperty]
    public partial double DaytimeBiasB { get; set; }

    [ObservableProperty]
    public partial int NighttimeGain { get; set; }

    [ObservableProperty]
    public partial double NighttimeElectronGain { get; set; }

    [ObservableProperty]
    public partial double NighttimeBiasR { get; set; }

    [ObservableProperty]
    public partial double NighttimeBiasG { get; set; }

    [ObservableProperty]
    public partial double NighttimeBiasB { get; set; }

    [ObservableProperty]
    public partial double TargetMedian { get; set; }
}
