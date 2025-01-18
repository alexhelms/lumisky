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
    IPublishSettings Publish { get; }
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
                new InterfaceConverter<IPublishSettings, PublishSettings>(),
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
        Publish.PropertyChanged += OnPropertyChanged;
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
        Publish.PropertyChanged -= OnPropertyChanged;
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
        Publish = new PublishSettings();
    }

    [ObservableProperty]
    public partial string Name { get; set; } = "default";

    [ObservableProperty]
    public partial string Description { get; set; } = "lumisky profile";

    [ObservableProperty]
    public partial DateTime LastActivatedUtc { get; set; }

    [ObservableProperty]
    public partial IAppSettings App { get; set; } = new AppSettings();

    [ObservableProperty]
    public partial ICameraSettings Camera { get; set; } = new CameraSettings();

    [ObservableProperty]
    public partial ICaptureSettings Capture { get; set; } = new CaptureSettings();

    [ObservableProperty]
    public partial IExportSettings Export { get; set; } = new ExportSettings();

    [ObservableProperty]
    public partial IImageSettings Image { get; set; } = new ImageSettings();

    [ObservableProperty]
    public partial ILocationSettings Location { get; set; } = new LocationSettings();

    [ObservableProperty]
    public partial IProcessingSettings Processing { get; set; } = new ProcessingSettings();

    [ObservableProperty]
    public partial IGenerationSettings Generation { get; set; } = new GenerationSettings();

    [ObservableProperty]
    public partial IPublishSettings Publish { get; set; } = new PublishSettings();
}
