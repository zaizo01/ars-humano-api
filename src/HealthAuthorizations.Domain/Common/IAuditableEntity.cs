namespace HealthAuthorizations.Domain.Common;

/// <summary>
/// Entidades cuyos campos de auditoría se completan automáticamente al persistir.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
