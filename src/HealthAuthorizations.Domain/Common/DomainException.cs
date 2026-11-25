namespace HealthAuthorizations.Domain.Common;

/// <summary>
/// Violación de una regla de negocio detectada por el dominio.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
