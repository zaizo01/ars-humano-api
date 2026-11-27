namespace HealthAuthorizations.Application.Common;

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' no existe.");

public sealed class ConflictException(string message, Exception? inner = null)
    : Exception(message, inner);

public sealed class PreconditionFailedException(string message)
    : Exception(message);
