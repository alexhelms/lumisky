﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace LumiSky.Core.Profile;

public interface IAppSettings : ISettings
{
}

public sealed partial class AppSettings : Settings, IAppSettings
{
    protected override void Reset()
    {
    }
}