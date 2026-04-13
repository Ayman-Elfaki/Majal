namespace Majal.EfCoreSample;

[ValueObject<string>]
public readonly partial struct AuthorName
{
    public const int MaxLength = 255;
}