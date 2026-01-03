using System.Net;

namespace Tuna.SuperTemplate.Exception;

public class BadRequestException : CustomException
{
    public BadRequestException(string message, int? code = null) : base(message, HttpStatusCode.BadRequest, code: code)
    {

    }
}