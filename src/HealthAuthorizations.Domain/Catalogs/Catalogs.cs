namespace HealthAuthorizations.Domain.Catalogs;

public sealed class ProviderType
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public sealed class ServiceType
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Intermediary
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Contractor
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string TaxId { get; set; }
    public int? IntermediaryId { get; set; }
    public Intermediary? Intermediary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Affiliate
{
    public int Id { get; set; }
    public required string AffiliateNumber { get; set; }
    public required string DocumentNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public int ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Provider
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string TaxId { get; set; }
    public int ProviderTypeId { get; set; }
    public ProviderType? ProviderType { get; set; }
    public bool IsActive { get; set; } = true;
}
