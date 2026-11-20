namespace RaceDay.Api.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "RaceDay.Api";
    public string Audience { get; set; } = "RaceDay.Clients";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int SessionDays { get; set; } = 7;
}
