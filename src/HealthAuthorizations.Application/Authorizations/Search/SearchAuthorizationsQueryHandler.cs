using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.Search;

/// <summary>
/// Paginación por cursor (keyset) sobre <c>Number</c> descendente (más recientes primero):
/// <c>WHERE "Number" &lt; @cursor ORDER BY "Number" DESC LIMIT n+1</c>.
/// A diferencia de OFFSET, el costo no crece con la profundidad de la página y no se
/// saltan ni duplican filas cuando se insertan registros mientras el cliente pagina.
/// </summary>
public sealed class SearchAuthorizationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchAuthorizationsQuery, CursorPage<AuthorizationSummaryResponse>>
{
    public async Task<CursorPage<AuthorizationSummaryResponse>> Handle(SearchAuthorizationsQuery request, CancellationToken cancellationToken)
    {
        var authorizations = ApplyFilters(db.Authorizations.AsNoTracking(), request);

        // Se pide un elemento extra solo para saber si existe una página siguiente.
        var rows = await (
                from a in authorizations
                join affiliate in db.Affiliates on a.AffiliateId equals affiliate.Id
                join provider in db.Providers on a.ProviderId equals provider.Id
                orderby a.Number descending
                select new AuthorizationSummaryResponse
                {
                    Id = a.Id,
                    Number = a.Number,
                    AffiliateId = a.AffiliateId,
                    AffiliateName = affiliate.FirstName + " " + affiliate.LastName,
                    ProviderId = a.ProviderId,
                    ProviderName = provider.Name,
                    ContractorId = a.ContractorId,
                    ServiceDate = a.ServiceDate,
                    Status = a.Status,
                    ServicesCount = a.Details.Count,
                    TotalClaimed = a.TotalClaimed,
                    TotalCovered = a.TotalCovered,
                    TotalCopay = a.TotalCopay,
                })
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > request.PageSize;
        var items = hasMore ? rows.GetRange(0, request.PageSize) : rows;

        return new CursorPage<AuthorizationSummaryResponse>(items, hasMore ? items[^1].Number : null);
    }

    private static IQueryable<Authorization> ApplyFilters(IQueryable<Authorization> query, SearchAuthorizationsQuery r)
    {
        if (r.Number is { } number)
        {
            query = query.Where(a => a.Number == number);
        }

        if (r.AffiliateId is { } affiliateId)
        {
            query = query.Where(a => a.AffiliateId == affiliateId);
        }

        if (r.ProviderId is { } providerId)
        {
            query = query.Where(a => a.ProviderId == providerId);
        }

        if (r.ContractorId is { } contractorId)
        {
            query = query.Where(a => a.ContractorId == contractorId);
        }

        if (r.IntermediaryId is { } intermediaryId)
        {
            query = query.Where(a => a.IntermediaryId == intermediaryId);
        }

        if (r.ServiceTypeId is { } serviceTypeId)
        {
            query = query.Where(a => a.Details.Any(d => d.ServiceTypeId == serviceTypeId));
        }

        if (r.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (r.FromDate is { } from)
        {
            query = query.Where(a => a.ServiceDate >= from);
        }

        if (r.ToDate is { } to)
        {
            query = query.Where(a => a.ServiceDate <= to);
        }

        if (r.Cursor is { } cursor)
        {
            query = query.Where(a => a.Number < cursor);
        }

        return query;
    }
}
