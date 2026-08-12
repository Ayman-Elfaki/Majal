namespace Majal.EntityFrameworkCore.Common;

internal static class EfCoreConstants
{
    public const string EfCoreNamespace = "global::Microsoft.EntityFrameworkCore";
    public const string EfCoreDiagnosticsNamespace = $"{EfCoreNamespace}.Diagnostics";
    public const string EfCoreBuilders = $"{EfCoreNamespace}.Metadata.Builders";
    public const string EfCoreConventions = $"{EfCoreNamespace}.Metadata.Conventions";
    public const string EfCoreValueConversion = $"{EfCoreNamespace}.Storage.ValueConversion";
}