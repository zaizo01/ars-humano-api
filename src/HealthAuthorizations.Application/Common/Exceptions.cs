namespace HealthAuthorizations.Application.Common;

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' no existe.");

/// <summary>Conflicto de concurrencia o de unicidad al persistir.</summary>
public sealed class ConflictException(string message, Exception? inner = null)
    : Exception(message, inner);
