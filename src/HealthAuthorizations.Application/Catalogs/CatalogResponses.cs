namespace HealthAuthorizations.Application.Catalogs;

/// <summary>Elemento de catálogo simple (tipos de prestador, tipos de servicio, intermediarios).</summary>
public sealed record CatalogItemResponse(int Id, string Code, string Name, bool IsActive);

public sealed record ContractorResponse(int Id, string Code, string Name, string TaxId, int? IntermediaryId, bool IsActive);

public sealed record AffiliateResponse(
    int Id,
    string AffiliateNumber,
    string DocumentNumber,
    string FullName,
    DateOnly BirthDate,
    int ContractorId,
    bool IsActive);

public sealed record ProviderResponse(
    int Id,
    string Code,
    string Name,
    string TaxId,
    int ProviderTypeId,
    string ProviderTypeName,
    bool IsActive);
