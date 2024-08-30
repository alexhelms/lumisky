using OdinEye.Core.Collections;

namespace OdinEye.Core.Profile;

public interface IDeviceSettings : ISettings
{
    string Name { get; set; }
    ObservableDictionary<string, string> Extra { get; set; }

    string? GetExtraOrNull(string key)
    {
        if (Extra.TryGetValue(key, out var value)) return value;
        return null;
    }

    void SetExtra(string key, string? value) => Extra[key] = value ?? string.Empty;
}
