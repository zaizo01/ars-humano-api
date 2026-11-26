using HealthAuthorizations.Api.Http;
using HealthAuthorizations.Application.Authorizations;
using HealthAuthorizations.Application.Authorizations.Create;
using HealthAuthorizations.Application.Authorizations.GetById;
using HealthAuthorizations.Application.Authorizations.Search;
using HealthAuthorizations.Application.Authorizations.Update;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HealthAuthorizations.Api.Controllers;

[ApiController]
[Route("api/v1/authorizations")]
[Authorize(Policy = Policies.AuthorizationsRead)]
[Produces("application/json")]
public sealed class AuthorizationsController(ISender sender) : ControllerBase
{
    /// <summary>Registra una autorización. Envíe <c>Idempotency-Key</c> para que los reintentos no generen duplicados.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AuthorizationsWrite)]
    [ProducesResponseType<AuthorizationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        CreateAuthorizationCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { IdempotencyKey = idempotencyKey }, cancellationToken);
        SetETag(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Busca autorizaciones (más recientes primero) con paginación por cursor.
    /// Para la página siguiente envíe el <c>nextCursor</c> recibido en el parámetro <c>cursor</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<CursorPage<AuthorizationSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CursorPage<AuthorizationSummaryResponse>>> Search(
        [FromQuery] SearchAuthorizationsQuery query,
        CancellationToken cancellationToken) =>
        await sender.Send(query, cancellationToken);

    /// <summary>Obtiene el detalle de una autorización. El header <c>ETag</c> trae la versión para actualizarla.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AuthorizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorizationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAuthorizationByIdQuery(id), cancellationToken);
        SetETag(result);
        return result;
    }

    /// <summary>
    /// Reemplaza los datos y servicios de una autorización activa y recalcula sus montos.
    /// Requiere <c>If-Match</c> con el <c>ETag</c> obtenido al consultarla, para no pisar cambios de otro usuario.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AuthorizationsWrite)]
    [ProducesResponseType<AuthorizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<AuthorizationResponse>> Update(
        Guid id,
        UpdateAuthorizationCommand command,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return Problem(
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "Falta el header If-Match.",
                detail: "Envíe el ETag obtenido al consultar la autorización.");
        }

        if (!ETag.TryParse(ifMatch, out var expectedVersion))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "El header If-Match no es un ETag válido.");
        }

        var result = await sender.Send(command with { Id = id, ExpectedVersion = expectedVersion }, cancellationToken);
        SetETag(result);
        return result;
    }

    // Pendiente: DELETE /{id} (inactivación lógica).

    private void SetETag(AuthorizationResponse authorization) =>
        Response.Headers[HeaderNames.ETag] = ETag.From(authorization.Version);
}
