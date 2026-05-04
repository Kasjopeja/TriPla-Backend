namespace TriPla.Backend.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TriPla";
    public string Audience { get; set; } = "TriPla";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
