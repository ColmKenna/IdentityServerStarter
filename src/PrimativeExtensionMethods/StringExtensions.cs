using System.Text;

namespace PrimativeExtensionMethods;

public static class StringExtensions
{
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.Length == 1)
            return str.ToLower();

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
    
    public static string ConCat(this string str, params string[] strings)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(str);
        foreach (string s in strings)
        {
            sb.Append(s);
        }

        return sb.ToString();
    }
}