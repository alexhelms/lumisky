namespace LumiSky.Rpicam.Common;

public record RpicamResult(int ExitCode, TimeSpan Elapsed, string Stdout, string Stderr);
