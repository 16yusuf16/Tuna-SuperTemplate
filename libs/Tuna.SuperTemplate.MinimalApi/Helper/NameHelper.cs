namespace Tuna.SuperTemplate.MinimalApi.Helper;

public static class NameHelper
{
    public static string ToReadableName(this Type type)
    {
        var name = type.Name;
        return string.Concat(name.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Trim();
    }
}