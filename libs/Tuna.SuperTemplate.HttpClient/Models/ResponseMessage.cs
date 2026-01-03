using System.Net;

namespace Tuna.SuperTemplate.HttpClient.Models;

public class ResponseMessage<T>
{
    public ResponseMessage(T message,HttpStatusCode statusCode)
    {
        Message = message;
        StatusCode = statusCode;
    }

    public ResponseMessage(HttpResponseException httpResponseException, HttpStatusCode statusCode)
    {
        ExceptionMessages = [new HttpResponseException(httpResponseException.Code,httpResponseException.Message)];
        StatusCode = statusCode;
    }
    public ResponseMessage(List<HttpResponseException> httpResponseExceptions, HttpStatusCode statusCode)
    {
        ExceptionMessages = httpResponseExceptions;
        StatusCode = statusCode;
    }
    public T Message { get; }
    public HttpStatusCode StatusCode { get;  }
    public List<HttpResponseException> ExceptionMessages { get; }
    public bool IsError => ExceptionMessages?.Count > 0;
    public bool IsSuccess => StatusCode == HttpStatusCode.OK && !IsError;


}
