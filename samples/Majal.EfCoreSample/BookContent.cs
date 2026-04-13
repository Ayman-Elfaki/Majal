namespace Majal.EfCoreSample;

[ValueObject<string>]
public readonly partial struct BookContent
{
    public const int MaxLength = 255;
}