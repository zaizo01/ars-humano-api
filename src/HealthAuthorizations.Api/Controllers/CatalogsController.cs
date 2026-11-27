using HealthAuthorizations.Application.Catalogs;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorizations.Api.Controllers;

/// <summary>Catálogos de referencia (solo lectura).</summary>
[ApiController]
[Route("api/v1/catalogs")]
[Authorize(Policy = Policies.AuthorizationsRead)]
[Produces("application/json")]
public sealed class CatalogsController(ISender sender) : ControllerBase
{
    /// <summary>Tipos de prestador (médico, farmacia, clínica, laboratorio).</summary>
    [HttpGet("provider-types")]
    public Task<IReadOnlyList<CatalogItemResponse>> GetProviderTypes(CancellationToken cancellationToken) =>
        sender.Send(new GetReferenceCatalogQuery(ReferenceCatalog.ProviderTypes), cancellationToken);

    /// <summary>Tipos de servicio autorizables (consulta, emergencia, hospitalización, laboratorio, imágenes).</summary>
    [HttpGet("service-types")]
    public Task<IReadOnlyList<CatalogItemResponse>> GetServiceTypes(CancellationToken cancellationToken) =>
        sender.Send(new GetReferenceCatalogQuery(ReferenceCatalog.ServiceTypes), cancellationToken);

    /// <summary>Intermediarios.</summary>
    [HttpGet("intermediaries")]
    public Task<IReadOnlyList<CatalogItemResponse>> GetIntermediaries(CancellationToken cancellationToken) =>
        sender.Send(new GetReferenceCatalogQuery(ReferenceCatalog.Intermediaries), cancellationToken);

    /// <summary>Contratantes o clientes.</summary>
    [HttpGet("contractors")]
    public Task<IReadOnlyList<ContractorResponse>> GetContractors(CancellationToken cancellationToken) =>
        sender.Send(new GetContractorsQuery(), cancellationToken);

    /// <summary>Busca afiliados por número de afiliado, documento o nombre, con paginación por cursor.</summary>
    [HttpGet("affiliates")]
    [ProducesResponseType<CursorPage<AffiliateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<CursorPage<AffiliateResponse>> SearchAffiliates([FromQuery] SearchAffiliatesQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

    /// <summary>Busca prestadores por código, RNC o nombre, con paginación por cursor.</summary>
    [HttpGet("providers")]
    [ProducesResponseType<CursorPage<ProviderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<CursorPage<ProviderResponse>> SearchProviders([FromQuery] SearchProvidersQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);
}
