using System.IO;
using System.Text;

namespace Majal.Common;

internal static class Sources
{
    internal static readonly string AggregateAttributeSource =
        LoadMarkersForEmitting("AggregateAttribute");
    
    internal static readonly string AggregateMarkerInterfaceSource =
        LoadMarkersForEmitting("IAggregate");
    
    internal static readonly string EntityAttributeSource =
        LoadMarkersForEmitting("EntityAttribute");
    
    internal static readonly string EntityConfigurationSource =
        LoadMarkersForEmitting("EntityConfiguration");
    
    internal static readonly string EntityMarkerInterfaceSource =
        LoadMarkersForEmitting("IEntity");

    internal static readonly string AuditableEntityMarkerInterfaceSource =
        LoadMarkersForEmitting("IAuditableEntity");

    internal static readonly string ArchivableEntityMarkerInterfaceSource =
        LoadMarkersForEmitting("IArchivableEntity");

    internal static readonly string OrdinalEntityMarkerInterfaceSource =
        LoadMarkersForEmitting("IOrdinalEntity");
    
    internal static readonly string ValueObjectAttributeSource =
        LoadMarkersForEmitting("ValueObjectAttribute");
    
    internal static readonly string ValueObjectMarkerInterfaceSource =
        LoadMarkersForEmitting("IValueObject");
    
    internal static readonly string SimpleValueObjectAttributeSource =
        LoadMarkersForEmitting("SimpleValueObjectAttribute");

    internal static readonly string SimpleValueObjectMarkerInterfaceSource =
        LoadMarkersForEmitting("ISimpleValueObject");
    
    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(Sources).Assembly;
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null) return string.Empty;
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string LoadMarkersForEmitting(string resourceName) => 
        LoadEmbeddedResource($"Majal.Markers.{resourceName}.cs");
}