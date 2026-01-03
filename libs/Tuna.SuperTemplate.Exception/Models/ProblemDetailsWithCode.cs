using System.Text.Json.Serialization;

namespace Tuna.SuperTemplate.Exception.Models;

public class ProblemDetailsWithCode : Microsoft.AspNetCore.Mvc.ProblemDetails
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }
}
