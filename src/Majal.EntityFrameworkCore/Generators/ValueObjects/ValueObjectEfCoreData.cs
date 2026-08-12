namespace Majal.Generators.ValueObjects;

public readonly record struct ValueObjectEfCoreData
{
    public string TypeName { get; }
    public string RawTypeName { get; }
    public string Namespace { get; }
    public string GenericType { get; }
    public int? MaxLength { get; }
    public bool IsStruct { get; }

    public ValueObjectEfCoreData(string typeName, string rawTypeName, string @namespace, string genericType,
        int? maxLength, bool isStruct)
    {
        TypeName = typeName;
        RawTypeName = rawTypeName;
        Namespace = @namespace;
        GenericType = genericType;
        MaxLength = maxLength;
        IsStruct = isStruct;
    }
}