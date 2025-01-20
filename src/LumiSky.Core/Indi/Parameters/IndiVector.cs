using LumiSky.Core.Indi.Serialization;
using Serilog;
using System.Globalization;

namespace LumiSky.Core.Indi.Parameters;

public abstract class IndiVector
{
    protected IndiVector(DefVector def)
    {
        Device = def.Device;
        Name = def.Name;
        Group = def.Group;
        Label = def.Label;
        State = def.State;
        Permission = def.Permission;
        Timeout = def.Timeout;
        Timestamp = ParseRawTimestamp(def.Timestamp);
    }

    public string Device { get; }
    public string Name { get; }
    public string? Group { get; private set; }
    public string? Label { get; private set; }
    public PropertyState State { get; set; }
    public PropertyPermission Permission { get; private set; }
    public int? Timeout { get; protected set; }
    public DateTime? Timestamp { get; protected set; }

    protected static DateTime? ParseRawTimestamp(string? rawTimestamp)
    {
        if (string.IsNullOrWhiteSpace(rawTimestamp))
            return null;

        DateTime.TryParse(rawTimestamp.Trim(), null, DateTimeStyles.RoundtripKind, out var timestamp);
        timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        return timestamp;
    }

    protected static double ParseRawNumber(string rawNumber, string format)
    {
        try
        {
            rawNumber = rawNumber.Trim();
            if (rawNumber.Contains(' ') || rawNumber.Contains(':') || rawNumber.Contains(';'))
            {
                // Sexagesimal
                string[] elements = rawNumber.Trim().Split([' ', ':', ';'], 3, StringSplitOptions.TrimEntries);
                if (elements.Length == 1)
                {

                }
                else if (elements.Length == 2)
                {

                }
                else if (elements.Length == 3)
                {
                    string a = elements[0];
                    string b = elements[1];
                    string c = elements[2];

                    double.TryParse(a, out double hour);
                    double.TryParse(b, out double min);
                    double.TryParse(c, out double sec);

                    double total = hour + (min / 60.0) + (sec / 3600.0);
                    return total;
                }
            }
            else
            {
                double.TryParse(rawNumber, out double value);
                return value;
            }
        }
        catch (Exception)
        {
            Log.Error("Error parsing INDI number: {Number}", rawNumber);
        }

        return 0;
    }

    protected static SwitchState ParseRawSwitchState(string rawSwitchState)
    {
        Enum.TryParse<SwitchState>(rawSwitchState.Trim(), out var state);
        return state;
    }

    public abstract IIndiCommand ToCommand(IEnumerable<(string, object)> values);
}
