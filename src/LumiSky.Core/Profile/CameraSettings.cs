﻿using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.Collections;
using System.Collections.Specialized;

namespace LumiSky.Core.Profile;

public interface ICameraSettings : IDeviceSettings
{
    string IndiHostname { get; set; }

    int IndiPort { get; set; }

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
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IndiHostname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int IndiPort { get; set; } = 7624;

    [ObservableProperty]
    public partial ObservableDictionary<string, string> Extra { get; set; } = new();

    [ObservableProperty]
    public partial double PixelSize { get; set; }

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
