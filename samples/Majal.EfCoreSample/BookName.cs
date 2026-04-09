namespace Majal.EfCoreSample;

[ValueObject<string>]
public partial class BookName
{
    public const int MaxLength = 255;

    public static BookName Create(string value)
    {
        return new BookName
        {
            Value = value
        };
    }

    public override string ToString() => Value;
}