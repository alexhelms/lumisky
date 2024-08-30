using CommunityToolkit.Mvvm.ComponentModel;
using OdinEye.Core.Collections;
using System.Collections.Specialized;

namespace OdinEye.Core.Profile;

public interface ICameraSettings : IDeviceSettings
{
    string IndiHostname { get; set; }

    int IndiPort { get; set; }

    double FocalLength { get; set; }

    int Offset { get; set; }

    int DaytimeGain { get; set; }

    double DaytimeElectronGain { get; set; }

    double DaytimeBias { get; set; }
    
    int NighttimeGain { get; set; }

    double NighttimeElectronGain { get; set; }

    double NighttimeBias { get; set; }

    double TargetMedian { get; set; }
}

public sealed partial class CameraSettings : Settings, ICameraSettings
{
    protected override void HookEvents()
    {
        foreach (var (key, value) in Extra) HookPropertyEvents(value);

        ((INotifyCollectionChanged)Extra).CollectionChanged += OnCollectionChanged;
    }

    protected override void UnhookEvents()
    {
        foreach (var (key, value) in Extra) UnhookPropertyEvents(value);

        ((INotifyCollectionChanged)Extra).CollectionChanged -= OnCollectionChanged;
    }

    protected override void Reset()
    {
        Name = string.Empty;
        IndiHostname = "localhost";
        IndiPort = 7624;
        Extra = new();
        FocalLength = 10;
        Offset = 0;
        DaytimeGain = 0;
        DaytimeElectronGain = 1;
        DaytimeBias = 0;
        NighttimeGain = 0;
        NighttimeElectronGain = 1;
        NighttimeBias = 0;
        TargetMedian = 1000;
    }

    [ObservableProperty] string _name = string.Empty;
    [ObservableProperty] string _indiHostname = string.Empty;
    [ObservableProperty] int _indiPort = 7624;
    [ObservableProperty] ObservableDictionary<string, string> _extra = new();
    [ObservableProperty] double _pixelSize;
    [ObservableProperty] double _focalLength;
    [ObservableProperty] int _offset;
    [ObservableProperty] int _daytimeGain;
    [ObservableProperty] double _daytimeElectronGain;
    [ObservableProperty] double _daytimeBias;
    [ObservableProperty] int _nighttimeGain;
    [ObservableProperty] double _nighttimeElectronGain;
    [ObservableProperty] double _nighttimeBias;
    [ObservableProperty] double _targetMedian;
}
