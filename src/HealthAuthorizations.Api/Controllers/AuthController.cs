using HealthAuthorizations.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorizations.Api.Controllers;

public sealed record TokenRequest(string UserName, string Password);

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(JwtTokenService tokenService) : ControllerBase
{
    /// <summary>Emite un JWT para un usuario de demostración.</summary>
    [HttpPost("token")]
    [ProducesResponseType<IssuedToken>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token(TokenRequest request)
    {
        var token = tokenService.TryIssue(request.UserName, request.Password);
        return token is null ? Unauthorized() : Ok(token);
    }
}
