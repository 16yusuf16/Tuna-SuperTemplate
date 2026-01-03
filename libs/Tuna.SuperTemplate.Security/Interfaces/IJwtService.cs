using Tuna.SuperTemplate.Security.Models;

namespace Tuna.SuperTemplate.Security.Interfaces;

public interface IJwtService
{
    string GenerateToken(TokenPayload payload);
    TokenPayload? ValidateToken(string token);
}
