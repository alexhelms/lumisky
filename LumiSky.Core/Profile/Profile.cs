using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using LumiSky.Core.Serialization.Converters;
using System.ComponentModel;

namespace LumiSky.Core.Profile;

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
    IGenerationSettings Generation { get; }
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
                new InterfaceConverter<IGenerationSettings, GenerationSettings>(),
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
        Generation.PropertyChanged += OnPropertyChanged;
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
        Generation.PropertyChanged -= OnPropertyChanged;
    }

    protected override void Reset()
    {
        Name = "default";
        Description = "lumisky profile";
        LastActivatedUtc = DateTime.MinValue;
        App = new AppSettings();
        Camera = new CameraSettings();
        Capture = new CaptureSettings();
        Export = new ExportSettings();
        Image = new ImageSettings();
        Location = new LocationSettings();
        Processing = new ProcessingSettings();
        Generation = new GenerationSettings();
    }

    [ObservableProperty] string _name = "default";
    [ObservableProperty] string _description = "lumisky profile";
    [ObservableProperty] DateTime _lastActivatedUtc;
    [ObservableProperty] IAppSettings _app = new AppSettings();
    [ObservableProperty] ICameraSettings _camera = new CameraSettings();
    [ObservableProperty] ICaptureSettings _capture = new CaptureSettings();
    [ObservableProperty] IExportSettings _export = new ExportSettings();
    [ObservableProperty] IImageSettings _image = new ImageSettings();
    [ObservableProperty] ILocationSettings _location = new LocationSettings();
    [ObservableProperty] IProcessingSettings _processing = new ProcessingSettings();
    [ObservableProperty] IGenerationSettings _generation = new GenerationSettings();
}
