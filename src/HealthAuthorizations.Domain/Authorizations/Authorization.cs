using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Authorizations;

public enum AuthorizationStatus
{
    Active = 1,
    Inactive = 2,
}

/// <summary>Partes involucradas en una autorización.</summary>
public sealed record AuthorizationParties(int AffiliateId, int ProviderId, int ContractorId, int? IntermediaryId);

/// <summary>Línea de servicio solicitada (antes de calcular la distribución).</summary>
public sealed record ServiceLine(int ServiceTypeId, string? Description, decimal ClaimedAmount);

/// <summary>
/// Agregado raíz. Garantiza: al menos un servicio, montos no negativos,
/// totales consistentes con el detalle y que una autorización inactiva no se modifica.
/// </summary>
public sealed class Authorization : IAuditableEntity, IHasDomainEvents
{
    private readonly List<AuthorizationDetail> _details = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Authorization()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Número correlativo legible, tomado de una secuencia de la base de datos.</summary>
    public long Number { get; private set; }

    public int AffiliateId { get; private set; }
    public int ProviderId { get; private set; }
    public int ContractorId { get; private set; }
    public int? IntermediaryId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public string? Notes { get; private set; }
    public AuthorizationStatus Status { get; private set; }

    public decimal TotalClaimed { get; private set; }
    public decimal TotalCovered { get; private set; }
    public decimal TotalCopay { get; private set; }

    /// <summary>Clave enviada por el cliente para evitar duplicados por reintentos.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Token de concurrencia optimista (xmin en PostgreSQL).</summary>
    public uint Version { get; private set; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public IReadOnlyCollection<AuthorizationDetail> Details => _details.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static Authorization Create(
        long number,
        AuthorizationParties parties,
        DateOnly serviceDate,
        string? notes,
        IReadOnlyCollection<ServiceLine> services,
        IAmountDistributionPolicy distributionPolicy,
        string? idempotencyKey = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);

        var authorization = new Authorization
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Status = AuthorizationStatus.Active,
            IdempotencyKey = idempotencyKey,
        };

        authorization.Apply(parties, serviceDate, notes, services, distributionPolicy);
        authorization._domainEvents.Add(new AuthorizationCreated(
            authorization.Id,
            authorization.Number,
            authorization.AffiliateId,
            authorization.ProviderId,
            authorization.ContractorId,
            authorization.IntermediaryId,
            authorization.ServiceDate,
            authorization.TotalClaimed,
            authorization.TotalCovered,
            authorization.TotalCopay));

        return authorization;
    }

    public void Update(
        AuthorizationParties parties,
        DateOnly serviceDate,
        string? notes,
        IReadOnlyCollection<ServiceLine> services,
        IAmountDistributionPolicy distributionPolicy)
    {
        EnsureActive();
        Apply(parties, serviceDate, notes, services, distributionPolicy);
        _domainEvents.Add(new AuthorizationUpdated(Id, Number, ProviderId, ServiceDate, TotalClaimed, TotalCovered, TotalCopay));
    }

    /// <summary>Inactivación lógica: el registro se conserva para fines históricos.</summary>
    public void Deactivate(DateTimeOffset at)
    {
        EnsureActive();
        Status = AuthorizationStatus.Inactive;
        DeactivatedAt = at;
        _domainEvents.Add(new AuthorizationDeactivated(Id, Number, at));
    }

    private void Apply(
        AuthorizationParties parties,
        DateOnly serviceDate,
        string? notes,
        IReadOnlyCollection<ServiceLine> services,
        IAmountDistributionPolicy distributionPolicy)
    {
        ArgumentNullException.ThrowIfNull(parties);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(distributionPolicy);

        if (services.Count == 0)
        {
            throw new DomainException("authorization.no_services", "La autorización debe contener al menos un servicio.");
        }

        AffiliateId = parties.AffiliateId;
        ProviderId = parties.ProviderId;
        ContractorId = parties.ContractorId;
        IntermediaryId = parties.IntermediaryId;
        ServiceDate = serviceDate;
        Notes = notes;

        _details.Clear();
        foreach (var line in services)
        {
            _details.Add(AuthorizationDetail.Create(Id, line, distributionPolicy));
        }

        TotalClaimed = _details.Sum(d => d.ClaimedAmount);
        TotalCovered = _details.Sum(d => d.CoveredAmount);
        TotalCopay = _details.Sum(d => d.CopayAmount);
    }

    private void EnsureActive()
    {
        if (Status == AuthorizationStatus.Inactive)
        {
            throw new DomainException("authorization.inactive", "La autorización está inactiva y no puede modificarse.");
        }
    }
}
