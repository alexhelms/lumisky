namespace System.Xml.Linq;

public static class XElementExtensions
{
    public static void AddOrUpdateAttribute(this XElement element, string attribute, object? value)
    {
        var attr = element.Attribute(attribute);
        if (attr is null)
            element.Add(new XAttribute(attribute, value?.ToString() ?? string.Empty));
        else
            attr.Value = value?.ToString() ?? string.Empty;
    }
}