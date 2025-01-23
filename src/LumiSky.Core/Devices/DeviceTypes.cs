namespace LumiSky.Core.Devices;

public static class DeviceTypes
{
    public const string INDI = "INDI";
    public const string RaspiWeb = "Raspi Web";
    public const string RaspiNative = "Raspi Native";

    public static IReadOnlyList<string> All => new List<string>
    {
        INDI,
        RaspiWeb,
#if Arm64
        RaspiNative,
#endif
    };
}
