using System.Text.Json;
using System.Text.Json.Serialization;

namespace LumiSky.Core.Indi;

public static class IndiMappings
{
    // Gain and Offset mappings were extracted from each vendor's indi drivers.
    // "None" implies the camera does not support the feature.
    // "" implies None, or a custom user-defined value.

    public static JsonSerializerOptions JsonOptions => new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    public static class Vendor
    {
        public const string ZWO = "ZWO";
        public const string QHY = "QHY";
        public const string ToupTek = "ToupTek";
        public const string PlayerOne = "PlayerOne";
        public const string Svbony = "Svbony";
        public const string Custom = "Custom";
    }
    
    public const string None = "None";

    public static IReadOnlyList<string> Vendors => [
        Vendor.ZWO,
        Vendor.QHY,
        Vendor.ToupTek,
        Vendor.PlayerOne,
        Vendor.Svbony,
        Vendor.Custom,
    ];

    public static IReadOnlyList<IndiPropertyMapping> GainMappings => [
        new(Vendor.ZWO, "CCD_CONTROLS:Gain"),
        new(Vendor.QHY, "CCD_GAIN:Gain"),
        new(Vendor.ToupTek, "CCD_CONTROLS:Gain"),
        new(Vendor.PlayerOne, "CCD_CONTROLS:Gain"),
        new(Vendor.Svbony, "CCD_CONTROLS:Gain"),
        new(Vendor.Custom, string.Empty),
    ];

    public static IReadOnlyList<IndiPropertyMapping> OffsetMappings => [
        new(Vendor.ZWO, "CCD_CONTROLS:Offset"),
        new(Vendor.QHY, "CCD_OFFSET:Offset"),
        new(Vendor.ToupTek, "CCD_OFFSET:Offset"),
        new(Vendor.PlayerOne, "CCD_CONTROLS:Offset"),
        new(Vendor.Svbony, "CCD_CONTROLS:Offset"),
        new(Vendor.Custom, string.Empty),
    ];
}

public record IndiPropertyMapping(string Vendor, string Mapping);

public record IndiCustomProperty(
    string Property,
    string Field,
    [property: JsonConverter(typeof(CustomIndiPropertyTypeConverter))] object Value);
