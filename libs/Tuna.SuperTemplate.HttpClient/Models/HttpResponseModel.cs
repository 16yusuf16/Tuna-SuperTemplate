using System.Net;

namespace Tuna.SuperTemplate.HttpClient.Models;

public  class HttpResponseModel
{
    public string? Content { get; set; } = string.Empty;
    public HttpStatusCode StatusCode { get; set; }
}
