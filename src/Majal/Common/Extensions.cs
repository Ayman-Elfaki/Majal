namespace Majal.Common;

public static class Extensions
{
    extension(string text)
    {
        public string SnakeCase => $"{char.ToLower(text[0])}{text.Substring(1)}";
    }
}