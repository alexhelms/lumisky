using CommunityToolkit.Mvvm.ComponentModel;

namespace OdinEye.Core.Profile;

public interface IExportSettings : ISettings
{
    bool EnableExport { get; set; }
    bool ExportRaws { get; set; }
    bool ExportImages { get; set; }
    bool ExportPanoramas { get; set; }
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
        FtpHostname = "localhost";
        FtpPort = 21;
        FtpUsername = "odineye";
        FtpPassword = "password";
        FtpRemotePath = string.Empty;
        EnableCertificateValidation = false;
    }

    [ObservableProperty] bool _enableExport;
    [ObservableProperty] bool _exportRaws;
    [ObservableProperty] bool _exportImages;
    [ObservableProperty] bool _exportPanoramas;
    [ObservableProperty] bool _enableFtp;
    [ObservableProperty] string _ftpHostname = "localhost";
    [ObservableProperty] int _ftpPort = 21;
    [ObservableProperty] string _ftpUsername = "odineye";
    [ObservableProperty] string _ftpPassword = "password";
    [ObservableProperty] string _ftpRemotePath = string.Empty;
    [ObservableProperty] bool _enableCertificateValidation;
}
