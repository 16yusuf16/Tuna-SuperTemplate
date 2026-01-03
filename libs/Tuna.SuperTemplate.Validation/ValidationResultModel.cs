using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Tuna.SuperTemplate.Validation;

public class ValidationResultModel
{
    public ValidationResultModel(FluentValidation.Results.ValidationResult? validationResult = null)
    {
        Errors = validationResult
            ?.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage))
            .ToList();
        Message = JsonSerializer.Serialize(Errors);
    }

    public int StatusCode { get; set; } = StatusCodes.Status400BadRequest;
    public string Message { get; set; }

    public IList<ValidationError>? Errors { get; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
