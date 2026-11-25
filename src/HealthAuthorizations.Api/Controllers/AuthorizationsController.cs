using HealthAuthorizations.Application.Authorizations;
using HealthAuthorizations.Application.Authorizations.Create;
using HealthAuthorizations.Application.Authorizations.GetById;
using HealthAuthorizations.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Obtiene el detalle de una autorización.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AuthorizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorizationResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new GetAuthorizationByIdQuery(id), cancellationToken);

    // Pendientes: GET (búsqueda paginada), PUT /{id} (con If-Match), DELETE /{id} (inactivación lógica).
}
