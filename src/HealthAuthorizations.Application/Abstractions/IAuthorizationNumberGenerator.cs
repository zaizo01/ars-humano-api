namespace HealthAuthorizations.Application.Abstractions;

// Se pide antes de guardar para que el evento authorization.created ya lleve el número.
public interface IAuthorizationNumberGenerator
{
    Task<long> NextAsync(CancellationToken cancellationToken);
}
