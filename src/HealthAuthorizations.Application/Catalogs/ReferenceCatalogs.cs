using HealthAuthorizations.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace HealthAuthorizations.Application.Catalogs;

// Catálogos pequeños y de lectura frecuente: se devuelven completos y se cachean.
// HybridCache evita la "estampida" (una sola consulta aunque lleguen muchas solicitudes a la vez)
// y permite agregar Redis como segundo nivel sin cambiar estos handlers.

public enum ReferenceCatalog
{
    ProviderTypes,
    ServiceTypes,
    Intermediaries,
}

public sealed record GetReferenceCatalogQuery(ReferenceCatalog Catalog) : IRequest<IReadOnlyList<CatalogItemResponse>>;

public sealed record GetContractorsQuery : IRequest<IReadOnlyList<ContractorResponse>>;

internal static class CatalogCache
{
    public const string Tag = "catalogs";

    public static readonly HybridCacheEntryOptions Options = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };
}

public sealed class GetReferenceCatalogQueryHandler(IApplicationDbContext db, HybridCache cache)
    : IRequestHandler<GetReferenceCatalogQuery, IReadOnlyList<CatalogItemResponse>>
{
    public async Task<IReadOnlyList<CatalogItemResponse>> Handle(GetReferenceCatalogQuery request, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            $"catalogs:{request.Catalog}",
            request.Catalog,
            (catalog, ct) => new ValueTask<List<CatalogItemResponse>>(Load(catalog).ToListAsync(ct)),
            CatalogCache.Options,
            [CatalogCache.Tag],
            cancellationToken);

    private IQueryable<CatalogItemResponse> Load(ReferenceCatalog catalog) => catalog switch
    {
        ReferenceCatalog.ProviderTypes => db.ProviderTypes.OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name, true)),
        ReferenceCatalog.ServiceTypes => db.ServiceTypes.OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name, x.IsActive)),
        ReferenceCatalog.Intermediaries => db.Intermediaries.OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name, x.IsActive)),
        _ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, "Catálogo no soportado."),
    };
}

public sealed class GetContractorsQueryHandler(IApplicationDbContext db, HybridCache cache)
    : IRequestHandler<GetContractorsQuery, IReadOnlyList<ContractorResponse>>
{
    public async Task<IReadOnlyList<ContractorResponse>> Handle(GetContractorsQuery request, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            "catalogs:contractors",
            async ct => await db.Contractors
                .OrderBy(x => x.Name)
                .Select(x => new ContractorResponse(x.Id, x.Code, x.Name, x.TaxId, x.IntermediaryId, x.IsActive))
                .ToListAsync(ct),
            CatalogCache.Options,
            [CatalogCache.Tag],
            cancellationToken);
}
