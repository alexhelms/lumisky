using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OdinEye.Core.Serialization.Converters;
using System.ComponentModel;

namespace OdinEye.Core.Profile;

public interface IProfile : INotifyPropertyChanged, INotifyPropertyChanging
{
    string Name { get; set; }
    string Description { get; set; }
    DateTime LastActivatedUtc { get; set; }
    IAppSettings App { get; }
    ICameraSettings Camera { get; }
    ICaptureSettings Capture { get; }
    IExportSettings Export { get; }
    IImageSettings Image { get; }
    ILocationSettings Location { get; }
    IProcessingSettings Processing { get; }
}

public sealed partial class Profile : Settings, IProfile
{
    internal static JsonSerializerSettings CreateSerializerSettings()
        => new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            FloatFormatHandling = FloatFormatHandling.String,
            FloatParseHandling = FloatParseHandling.Double,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(),
                new InterfaceConverter<IProfile, Profile>(),
                new InterfaceConverter<IAppSettings, AppSettings>(),
                new InterfaceConverter<ICameraSettings, CameraSettings>(),
                new InterfaceConverter<ICaptureSettings, CaptureSettings>(),
                new InterfaceConverter<IExportSettings, ExportSettings>(),
                new InterfaceConverter<IImageSettings, ImageSettings>(),
                new InterfaceConverter<ILocationSettings, LocationSettings>(),
                new InterfaceConverter<IProcessingSettings, ProcessingSettings>(),
            },
        };

    protected override void HookEvents()
    {
        App.PropertyChanged += OnPropertyChanged;
        Camera.PropertyChanged += OnPropertyChanged;
        Capture.PropertyChanged += OnPropertyChanged;
        Export.PropertyChanged += OnPropertyChanged;
        Image.PropertyChanged += OnPropertyChanged;
        Location.PropertyChanged += OnPropertyChanged;
        Processing.PropertyChanged += OnPropertyChanged;
    }

    protected override void UnhookEvents()
    {
        App.PropertyChanged -= OnPropertyChanged;
        Camera.PropertyChanged -= OnPropertyChanged;
        Capture.PropertyChanged -= OnPropertyChanged;
        Export.PropertyChanged -= OnPropertyChanged;
        Image.PropertyChanged -= OnPropertyChanged;
        Location.PropertyChanged -= OnPropertyChanged;
        Processing.PropertyChanged -= OnPropertyChanged;
    }

    protected override void Reset()
    {
        Name = "default";
        Description = "odineye profile";
        LastActivatedUtc = DateTime.MinValue;
        App = new AppSettings();
        Camera = new CameraSettings();
        Capture = new CaptureSettings();
        Export = new ExportSettings();
        Image = new ImageSettings();
        Location = new LocationSettings();
        Processing = new ProcessingSettings();
    }

    [ObservableProperty] string _name = "default";
    [ObservableProperty] string _description = "odineye profile";
    [ObservableProperty] DateTime _lastActivatedUtc;
    [ObservableProperty] IAppSettings _app = new AppSettings();
    [ObservableProperty] ICameraSettings _camera = new CameraSettings();
    [ObservableProperty] ICaptureSettings _capture = new CaptureSettings();
    [ObservableProperty] IExportSettings _export = new ExportSettings();
    [ObservableProperty] IImageSettings _image = new ImageSettings();
    [ObservableProperty] ILocationSettings _location = new LocationSettings();
    [ObservableProperty] IProcessingSettings _processing = new ProcessingSettings();
}
