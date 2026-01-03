using System.Text.Json.Serialization;

namespace Tuna.SuperTemplate.HttpClient.Models;

public sealed class HttpResponseException(string code, string message) :Exception ,IEquatable<HttpResponseException>
{
    [JsonPropertyName("message")]
    public string Message { get; } = message;
    [JsonPropertyName("code")]
    public string Code { get; } = code;

    public bool Equals(HttpResponseException? other)
    {
      return other is not null && other.Code == Code && other.Message == Message;
    }
    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(null,obj) ) return false;
        if(ReferenceEquals(this,obj)) return true;

        return obj.GetType() == GetType() && Equals((HttpResponseException)obj);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return (Message.GetHashCode() * 397) ^ Code.GetHashCode();
        }
    }
}
