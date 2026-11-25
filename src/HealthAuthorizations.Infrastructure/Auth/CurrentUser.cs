using HealthAuthorizations.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace HealthAuthorizations.Infrastructure.Auth;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserName => accessor.HttpContext?.User.Identity?.Name;
}
