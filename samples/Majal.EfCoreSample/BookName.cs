namespace Majal.EfCoreSample;

[ValueObject<string>]
public partial class BookName
{
    public static BookName Create(string value)
    {
        return new BookName
        {
            Value = value
        };
    }
}