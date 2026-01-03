using OneOf;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tuna.SuperTemplate.HybridCache.Helper;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions DefaultSettings = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static OneOf<string?,Exception> Seriliaze<T>(this T value, string? defaultValue, JsonSerializerOptions? options = null)
    {
        if(Equals(value, default(T))) return defaultValue;
        try
        {
            return JsonSerializer.Serialize(value, options ?? DefaultSettings);
            
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    public static OneOf<T?,Exception> DeSeriliaze<T>(this string? value, JsonSerializerOptions? options = null)
    {
        if(string.IsNullOrWhiteSpace(value)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(value, options ?? DefaultSettings);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
