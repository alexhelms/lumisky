using LumiSky.Core.Indi.Serialization;

namespace LumiSky.Core.Indi.Parameters;

public class IndiBlobVector : IndiVector
{
    public IndiBlobVector(DefBlobVector def)
        : base(def)
    {
        Items = def.Items
            .Select(x => new IndiBlob
            {
                Name = x.Name,
                Label = x.Label,
            })
            .ToDictionary(x => x.Name);
    }

    public Dictionary<string, IndiBlob> Items { get; private set; } = [];

    public void Update(SetBlobVector command)
    {
        Timeout = command.Timeout;
        Timestamp = ParseRawTimestamp(command.Timestamp);
        State = command.State;

        foreach (var newItem in command.Items)
        {
            if (Items.TryGetValue(newItem.Name, out var item))
            {
                item.Size = newItem.Size;
                item.Format = newItem.Format;
                item.Value = newItem.Value;
            }
        }
    }

    public override IIndiCommand ToCommand(IEnumerable<(string, object)> values)
    {
        throw new NotImplementedException();
    }
}

public class IndiBlob: IIndiValue
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public int Size { get; set; }
    public string Format { get; set; } = string.Empty;
    public Memory<byte> Value { get; set; } = Array.Empty<byte>();
}
