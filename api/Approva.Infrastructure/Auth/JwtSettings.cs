namespace Approva.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "Approva";
    public string Audience { get; set; } = "Approva";
    public int ExpiryMinutes { get; set; } = 480;
}
