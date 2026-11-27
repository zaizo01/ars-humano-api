using HealthAuthorizations.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;


internal static class CatalogSeed
{
    // Las identity arrancan después de los IDs del seed para no chocar.
    public const long IdentityStart = 1000;
}

internal sealed class ProviderTypeConfiguration : IEntityTypeConfiguration<ProviderType>
{
    public void Configure(EntityTypeBuilder<ProviderType> builder)
    {
        builder.ToTable("TiposPrestadores");
        builder.Property(x => x.Id).HasIdentityOptions(startValue: CatalogSeed.IdentityStart);
        builder.Property(x => x.Code).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new ProviderType { Id = 1, Code = "MED", Name = "Médico" },
            new ProviderType { Id = 2, Code = "FAR", Name = "Farmacia" },
            new ProviderType { Id = 3, Code = "CLI", Name = "Clínica" },
            new ProviderType { Id = 4, Code = "LAB", Name = "Laboratorio" });
    }
}

internal sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("TiposServicios");
        builder.Property(x => x.Id).HasIdentityOptions(startValue: CatalogSeed.IdentityStart);
        builder.Property(x => x.Code).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new ServiceType { Id = 1, Code = "CON", Name = "Consulta" },
            new ServiceType { Id = 2, Code = "EME", Name = "Emergencia" },
            new ServiceType { Id = 3, Code = "HOS", Name = "Hospitalización" },
            new ServiceType { Id = 4, Code = "LAB", Name = "Laboratorio" },
            new ServiceType { Id = 5, Code = "IMG", Name = "Imágenes" });
    }
}

internal sealed class IntermediaryConfiguration : IEntityTypeConfiguration<Intermediary>
{
    public void Configure(EntityTypeBuilder<Intermediary> builder)
    {
        builder.ToTable("Intermediarios");
        builder.Property(x => x.Id).HasIdentityOptions(startValue: CatalogSeed.IdentityStart);
        builder.Property(x => x.Code).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new Intermediary { Id = 1, Code = "INT-001", Name = "Corredora de Seguros Caribe" },
            new Intermediary { Id = 2, Code = "INT-002", Name = "Asesores Unidos" });
    }
}

internal sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("Contratantes");
        builder.Property(x => x.Id).HasIdentityOptions(startValue: CatalogSeed.IdentityStart);
        builder.Property(x => x.Code).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.TaxId).IsUnique();
        builder.HasOne(x => x.Intermediary).WithMany().HasForeignKey(x => x.IntermediaryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Contractor { Id = 1, Code = "CTR-001", Name = "Industrias del Norte SRL", TaxId = "101000001", IntermediaryId = 1 },
            new Contractor { Id = 2, Code = "CTR-002", Name = "Comercial Las Américas SA", TaxId = "101000002", IntermediaryId = 2 },
            new Contractor { Id = 3, Code = "CTR-003", Name = "Plan Individual", TaxId = "101000003" });
    }
}

internal sealed class AffiliateConfiguration : IEntityTypeConfiguration<Affiliate>
{
    public void Configure(EntityTypeBuilder<Affiliate> builder)
    {
        builder.ToTable("Afiliados");
        builder.Property(x => x.Id).HasIdentityOptions(startValue: CatalogSeed.IdentityStart);
        builder.Property(x => x.AffiliateNumber).HasMaxLength(20);
        builder.Property(x => x.DocumentNumber).HasMaxLength(20);
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.HasIndex(x => x.AffiliateNumber).IsUnique();
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Affiliate { Id = 1, AffiliateNumber = "AF-0001", DocumentNumber = "00100000001", FirstName = "María", LastName = "Pérez", BirthDate = new DateOnly(1985, 4, 12), ContractorId = 1 },
            new Affiliate { Id = 2, AffiliateNumber = "AF-0002", DocumentNumber = "00100000002", FirstName = "Juan", LastName = "Rodríguez", BirthDate = new DateOnly(1990, 9, 3), ContractorId = 1 },
            new Affiliate { Id = 3, AffiliateNumber = "AF-0003", DocumentNumber = "00100000003", FirstName = "Ana", LastName = "Martínez", BirthDate = new DateOnly(1978, 1, 25), ContractorId = 2 },
            new Affiliate { Id = 4, AffiliateNumber = "AF-0004", DocumentNumber = "00100000004", FirstName = "Luis", LastName = "Gómez", BirthDate = new DateOnly(2001, 6, 30), ContractorId = 3 },
            new Affiliate { Id = 5, AffiliateNumber = "AF-0005", DocumentNumber = "00100000005", FirstName = "Carmen", LastName = "Santos", BirthDate = new DateOnly(1965, 11, 8), ContractorId = 2, IsActive = false });
    }
}

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Prestadores");
        builder.Property(x => x.Id).HasIdentityOptions(startValue: CatalogSeed.IdentityStart);
        builder.Property(x => x.Code).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.ProviderTypeId);
        builder.HasOne(x => x.ProviderType).WithMany().HasForeignKey(x => x.ProviderTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Provider { Id = 1, Code = "PRE-001", Name = "Dr. Roberto Castillo", TaxId = "00200000001", ProviderTypeId = 1 },
            new Provider { Id = 2, Code = "PRE-002", Name = "Farmacia Central", TaxId = "130000001", ProviderTypeId = 2 },
            new Provider { Id = 3, Code = "PRE-003", Name = "Clínica Santa Lucía", TaxId = "130000002", ProviderTypeId = 3 },
            new Provider { Id = 4, Code = "PRE-004", Name = "Laboratorio Referencia", TaxId = "130000003", ProviderTypeId = 4 },
            new Provider { Id = 5, Code = "PRE-005", Name = "Centro Médico del Este", TaxId = "130000004", ProviderTypeId = 3 });
    }
}
