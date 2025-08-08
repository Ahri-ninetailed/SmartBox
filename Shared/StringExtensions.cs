namespace Shared
{
    public static class StringExtensions
    {
        public static string ToLowerFirstChar(this string input)
        {
            return char.ToLower(input[0]) + input.Substring(1);
        }
        public static string ToUpperFirstChar(this string input)
        {
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
