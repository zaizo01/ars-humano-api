using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

internal sealed class AuthorizationConfiguration : IEntityTypeConfiguration<Authorization>
{
    public const string NumberSequence = "AutorizacionesNumeroSeq";

    public void Configure(EntityTypeBuilder<Authorization> builder)
    {
        builder.ToTable("Autorizaciones", t =>
        {
            t.HasCheckConstraint("CK_Autorizaciones_Montos_NoNegativos",
                "\"TotalClaimed\" >= 0 AND \"TotalCovered\" >= 0 AND \"TotalCopay\" >= 0");
            t.HasCheckConstraint("CK_Autorizaciones_Montos_Cuadran",
                "\"TotalCovered\" + \"TotalCopay\" = \"TotalClaimed\"");
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Number)
            .HasDefaultValueSql($"nextval('\"{NumberSequence}\"')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(a => a.Number).IsUnique();

        builder.Property(a => a.Status).HasConversion<short>();
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.IdempotencyKey).HasMaxLength(100);
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.Property(a => a.TotalClaimed).HasPrecision(18, 2);
        builder.Property(a => a.TotalCovered).HasPrecision(18, 2);
        builder.Property(a => a.TotalCopay).HasPrecision(18, 2);

        // Concurrencia optimista usando la columna de sistema xmin de PostgreSQL.
        builder.Property(a => a.Version).IsRowVersion();

        builder.HasOne<Affiliate>().WithMany().HasForeignKey(a => a.AffiliateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Provider>().WithMany().HasForeignKey(a => a.ProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Contractor>().WithMany().HasForeignKey(a => a.ContractorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Intermediary>().WithMany().HasForeignKey(a => a.IntermediaryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Details)
            .WithOne()
            .HasForeignKey(d => d.AuthorizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Details).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Índices para los criterios de búsqueda frecuentes.
        builder.HasIndex(a => a.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasIndex(a => new { a.AffiliateId, a.ServiceDate });
        builder.HasIndex(a => new { a.ProviderId, a.ServiceDate });
        builder.HasIndex(a => new { a.Status, a.ServiceDate });
        builder.HasIndex(a => a.ContractorId);
        builder.HasIndex(a => a.IntermediaryId);
    }
}

internal sealed class AuthorizationDetailConfiguration : IEntityTypeConfiguration<AuthorizationDetail>
{
    public void Configure(EntityTypeBuilder<AuthorizationDetail> builder)
    {
        builder.ToTable("AutorizacionesDetalle", t =>
        {
            t.HasCheckConstraint("CK_AutorizacionesDetalle_MontoReclamado_Positivo", "\"ClaimedAmount\" > 0");
            t.HasCheckConstraint("CK_AutorizacionesDetalle_Montos_NoNegativos", "\"CoveredAmount\" >= 0 AND \"CopayAmount\" >= 0");
            t.HasCheckConstraint("CK_AutorizacionesDetalle_Montos_Cuadran", "\"CoveredAmount\" + \"CopayAmount\" = \"ClaimedAmount\"");
        });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseIdentityAlwaysColumn();
        builder.Property(d => d.Description).HasMaxLength(250);
        builder.Property(d => d.ClaimedAmount).HasPrecision(18, 2);
        builder.Property(d => d.CoveredAmount).HasPrecision(18, 2);
        builder.Property(d => d.CopayAmount).HasPrecision(18, 2);

        builder.HasOne<ServiceType>().WithMany().HasForeignKey(d => d.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.ServiceTypeId);
    }
}
