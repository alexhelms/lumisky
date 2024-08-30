using System.Xml.Linq;

namespace OdinEye.INDI.Protocol;

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