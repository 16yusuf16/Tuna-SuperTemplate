using System.Net;

namespace Tuna.SuperTemplate.Exception;

public class NotFoundException : CustomException
{
    public NotFoundException(string message, int? code = null) : base(message, HttpStatusCode.NotFound, code: code)
    {
    }
}