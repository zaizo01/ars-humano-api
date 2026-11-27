using FluentValidation;
using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Catalogs;

// Search: exacto por código o RNC, parcial por nombre.
public sealed record SearchProvidersQuery : IRequest<CursorPage<ProviderResponse>>
{
    public string? Search { get; init; }
    public int? ProviderTypeId { get; init; }
    public bool? IsActive { get; init; }
    public long? Cursor { get; init; }
    public int PageSize { get; init; } = PaginationRules.DefaultPageSize;
}

public sealed class SearchProvidersQueryValidator : AbstractValidator<SearchProvidersQuery>
{
    public SearchProvidersQueryValidator()
    {
        RuleFor(x => x.PageSize).ValidPageSize();
        RuleFor(x => x.Cursor).ValidCursor();
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class SearchProvidersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchProvidersQuery, CursorPage<ProviderResponse>>
{
    public async Task<CursorPage<ProviderResponse>> Handle(SearchProvidersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Providers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var lowered = term.ToLower();
            query = query.Where(p => p.Code == term || p.TaxId == term || p.Name.ToLower().Contains(lowered));
        }

        if (request.ProviderTypeId is { } providerTypeId)
        {
            query = query.Where(p => p.ProviderTypeId == providerTypeId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        if (request.Cursor is { } cursor)
        {
            query = query.Where(p => p.Id > cursor);
        }

        var rows = await query
            .OrderBy(p => p.Id)
            .Take(request.PageSize + 1)
            .Select(p => new ProviderResponse(p.Id, p.Code, p.Name, p.TaxId, p.ProviderTypeId, p.ProviderType!.Name, p.IsActive))
            .ToListAsync(cancellationToken);

        return CursorPage.From(rows, request.PageSize, p => p.Id);
    }
}
