using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Authorizations;

/// <summary>Servicio autorizado dentro de una autorización, con su reparto de montos.</summary>
public sealed class AuthorizationDetail
{
    private AuthorizationDetail()
    {
    }

    public long Id { get; private set; }
    public Guid AuthorizationId { get; private set; }
    public int ServiceTypeId { get; private set; }
    public string? Description { get; private set; }
    public decimal ClaimedAmount { get; private set; }
    public decimal CoveredAmount { get; private set; }
    public decimal CopayAmount { get; private set; }

    internal static AuthorizationDetail Create(Guid authorizationId, ServiceLine line, IAmountDistributionPolicy policy)
    {
        if (line.ClaimedAmount <= 0m)
        {
            throw new DomainException("amount.not_positive", "El monto reclamado de cada servicio debe ser mayor que cero.");
        }

        var distribution = policy.Distribute(line.ClaimedAmount);

        return new AuthorizationDetail
        {
            AuthorizationId = authorizationId,
            ServiceTypeId = line.ServiceTypeId,
            Description = line.Description,
            ClaimedAmount = line.ClaimedAmount,
            CoveredAmount = distribution.CoveredAmount,
            CopayAmount = distribution.CopayAmount,
        };
    }
}
