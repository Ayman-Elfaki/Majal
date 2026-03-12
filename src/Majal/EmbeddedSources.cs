using System.IO;
using System.Text;

namespace Majal;

internal static class EmbeddedSources
{
    internal static readonly string AggregateRootAttributeSource =
        LoadMarkersForEmitting("AggregateRootAttribute");
    
    internal static readonly string AggregateRootMarkerInterfaceSource =
        LoadMarkersForEmitting("IAggregateRoot");
    
    internal static readonly string EntityAttributeSource =
        LoadMarkersForEmitting("EntityAttribute");

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
    
    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(EmbeddedSources).Assembly;
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null) return string.Empty;
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string LoadMarkersForEmitting(string resourceName) => 
        LoadEmbeddedResource($"Majal.Markers.{resourceName}.cs");
}