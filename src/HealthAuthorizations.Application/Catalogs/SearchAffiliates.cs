using FluentValidation;
using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Catalogs;

/// <summary>
/// Afiliados: catálogo grande, por eso se busca y pagina en vez de devolverse completo.
/// <c>Search</c> coincide exacto con número de afiliado o documento, o parcial con el nombre.
/// </summary>
public sealed record SearchAffiliatesQuery : IRequest<CursorPage<AffiliateResponse>>
{
    public string? Search { get; init; }
    public int? ContractorId { get; init; }
    public bool? IsActive { get; init; }
    public long? Cursor { get; init; }
    public int PageSize { get; init; } = PaginationRules.DefaultPageSize;
}

public sealed class SearchAffiliatesQueryValidator : AbstractValidator<SearchAffiliatesQuery>
{
    public SearchAffiliatesQueryValidator()
    {
        RuleFor(x => x.PageSize).ValidPageSize();
        RuleFor(x => x.Cursor).ValidCursor();
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class SearchAffiliatesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchAffiliatesQuery, CursorPage<AffiliateResponse>>
{
    public async Task<CursorPage<AffiliateResponse>> Handle(SearchAffiliatesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Affiliates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var lowered = term.ToLower();
            query = query.Where(a =>
                a.AffiliateNumber == term ||
                a.DocumentNumber == term ||
                (a.FirstName + " " + a.LastName).ToLower().Contains(lowered));
        }

        if (request.ContractorId is { } contractorId)
        {
            query = query.Where(a => a.ContractorId == contractorId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(a => a.IsActive == isActive);
        }

        if (request.Cursor is { } cursor)
        {
            query = query.Where(a => a.Id > cursor);
        }

        var rows = await query
            .OrderBy(a => a.Id)
            .Take(request.PageSize + 1)
            .Select(a => new AffiliateResponse(
                a.Id, a.AffiliateNumber, a.DocumentNumber, a.FirstName + " " + a.LastName, a.BirthDate, a.ContractorId, a.IsActive))
            .ToListAsync(cancellationToken);

        return CursorPage.From(rows, request.PageSize, a => a.Id);
    }
}
