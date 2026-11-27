namespace HealthAuthorizations.Application.Abstractions;

public interface ICurrentUser
{
    string? UserName { get; }
}
