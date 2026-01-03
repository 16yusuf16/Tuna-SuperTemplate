namespace Tuna.SuperTemplate.Security.Models;

public class TokenPayload
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public Dictionary<string, string>? ExtraClaims { get; set; }
}
