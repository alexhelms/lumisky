namespace OdinEye.Core.IO;

public static class OdinEyePaths
{
    private static bool _basePathOverridden;

    public static string BasePath
    {
        get
        {
            var pathOverride = Environment.GetEnvironmentVariable("ODINEYE_PATH");
            if (pathOverride is not null)
            {
                // Only log this once.
                if (!_basePathOverridden)
                {
                    Log.Information("Base path overridden by environment variable: {Path}", pathOverride);
                    _basePathOverridden = true;
                }
                return pathOverride;
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".odineye");
        }
    }

    public static string Logs => Path.Combine(BasePath, "logs");

    public static string Profiles => Path.Combine(BasePath, "profiles");
}
