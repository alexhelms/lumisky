using CommunityToolkit.Mvvm.ComponentModel;

namespace LumiSky.Core.Profile;

public interface IExportSettings : ISettings
{
    bool EnableExport { get; set; }
    bool ExportRaws { get; set; }
    bool ExportImages { get; set; }
    bool ExportPanoramas { get; set; }
    bool ExportTimelapses { get; set; }
    bool ExportPanoramaTimelapses { get; set; }
    bool EnableFtp { get; set; }
    string FtpHostname { get; set; }
    int FtpPort { get; set; }
    string FtpUsername { get; set; }
    string FtpPassword { get; set; }
    string FtpRemotePath { get; set; }
    bool EnableCertificateValidation { get; set; }
}

public sealed partial class ExportSettings : Settings, IExportSettings
{
    protected override void Reset()
    {
        EnableExport = false;
        ExportRaws = false;
        ExportImages = true;
        ExportPanoramas = false;
        ExportTimelapses = false;
        ExportPanoramaTimelapses = false;
        FtpHostname = "localhost";
        FtpPort = 21;
        FtpUsername = "lumisky";
        FtpPassword = "password";
        FtpRemotePath = string.Empty;
        EnableCertificateValidation = false;
    }

    [ObservableProperty]
    public partial bool EnableExport { get; set; }

    [ObservableProperty]
    public partial bool ExportRaws { get; set; }

    [ObservableProperty]
    public partial bool ExportImages { get; set; }

    [ObservableProperty]
    public partial bool ExportPanoramas { get; set; }

    [ObservableProperty]
    public partial bool ExportTimelapses { get; set; }

    [ObservableProperty]
    public partial bool ExportPanoramaTimelapses { get; set; }

    [ObservableProperty]
    public partial bool EnableFtp { get; set; }

    [ObservableProperty]
    public partial string FtpHostname { get; set; } = "localhost";

    [ObservableProperty]
    public partial int FtpPort { get; set; } = 21;

    [ObservableProperty]
    public partial string FtpUsername { get; set; } = "lumisky";

    [ObservableProperty]
    public partial string FtpPassword { get; set; } = "password";

    [ObservableProperty]
    public partial string FtpRemotePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EnableCertificateValidation { get; set; }
}
