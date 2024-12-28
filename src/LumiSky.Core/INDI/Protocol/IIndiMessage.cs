using System.Xml.Linq;

namespace LumiSky.INDI.Protocol;

public interface IIndiMessage
{
    XElement ToXml();
}

public interface IIndiClientMessage : IIndiMessage
{
    // Empty
}

public interface IIndiServerMessage : IIndiMessage
{
    void Process(IndiConnection connection);
}