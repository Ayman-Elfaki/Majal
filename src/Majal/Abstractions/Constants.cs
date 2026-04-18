namespace Majal.Abstractions;

public static class Constants
{
    public const string MajalNamespace = "global::Majal";
    public const string SystemNamespace = "global::System";
    public const string JsonNamespace = "global::System.Text.Json";
    public const string DiagnosticsNamespace = "global::System.Diagnostics";
    public const string ExpressionsNamespace = "global::System.Linq.Expressions";
    public const string LinqNamespace = "global::System.Linq";
    public const string GenericCollectionNamespace = "global::System.Collections.Generic";
    public const string CollectionNamespace = "global::System.Collections";
    public const string ComponentModelNamespace = "global::System.ComponentModel";
    public const string GlobalizationNamespace = "global::System.Globalization";
    public const string ThreadingNamespace = "global::System.Threading";
    public const string TasksNamespace = $"{ThreadingNamespace}.Tasks";
    public const string JsonSerializationNamespace = $"{JsonNamespace}.Serialization";
    
    public const string ExpressionsType = $"{ExpressionsNamespace}.Expression";
    
    public const string EfCoreNamespace = "global::Microsoft.EntityFrameworkCore";
    public const string EfCoreDiagnosticsNamespace = $"{EfCoreNamespace}.Diagnostics";
    public const string EfCoreBuilders = $"{EfCoreNamespace}.Metadata.Builders";
    public const string EfCoreConventions = $"{EfCoreNamespace}.Metadata.Conventions";
    public const string EfCoreValueConversion = $"{EfCoreNamespace}.Storage.ValueConversion";


    public const string TypeType = "global::System.Type";
    public const string IntType = "global::System.Int32";
    public const string CharType = "global::System.Char";
    public const string UIntType = "global::System.UInt32";
    
    public const string BoolType = "global::System.Boolean";
    public const string StringType = "global::System.String";
    public const string ObjectType = "global::System.Object";
}