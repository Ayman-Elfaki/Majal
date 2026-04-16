namespace Majal.EfCoreSample;

[ValueObject<string>]
public readonly partial struct BookName
{
    public const int MaxLength = 1024;

    public static BookName Create(string value)
    {
        return new BookName
        {
            Value = value
        };
    }

    public override string ToString() => Value;
}

