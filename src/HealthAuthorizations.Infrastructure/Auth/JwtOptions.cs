namespace HealthAuthorizations.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    // Mínimo 32 caracteres.
    public required string SigningKey { get; init; }

    public int ExpirationMinutes { get; init; } = 60;
}

public sealed class DemoUserOptions
{
    public const string SectionName = "Auth:DemoUsers";

    public required string UserName { get; init; }
    public required string Password { get; init; }
    public string[] Roles { get; init; } = [];
}

public static class Roles
{
    public const string Operator = "Operador";
    public const string Auditor = "Consulta";
}

public static class Policies
{
    public const string AuthorizationsRead = "authorizations:read";
    public const string AuthorizationsWrite = "authorizations:write";
}
