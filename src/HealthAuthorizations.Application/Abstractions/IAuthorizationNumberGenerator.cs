namespace HealthAuthorizations.Application.Abstractions;

/// <summary>
/// Entrega el próximo número correlativo de autorización. Se obtiene antes de guardar para que
/// el evento <c>authorization.created</c> ya lo incluya.
/// </summary>
public interface IAuthorizationNumberGenerator
{
    Task<long> NextAsync(CancellationToken cancellationToken);
}
