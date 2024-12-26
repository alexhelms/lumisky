﻿namespace LumiSky.Core.IO;

public static class LumiSkyPaths
{
    private static bool _basePathOverridden;

    public static string BasePath
    {
        get
        {
            var pathOverride = Environment.GetEnvironmentVariable("LUMISKY_PATH");
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
            return Path.Combine(home, ".lumisky");
        }
    }

    public static string Logs => Path.Combine(BasePath, "logs");

    public static string Profiles => Path.Combine(BasePath, "profiles");
}