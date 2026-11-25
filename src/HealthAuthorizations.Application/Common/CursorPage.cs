namespace HealthAuthorizations.Application.Common;

/// <summary>
/// Página de resultados con paginación por cursor (keyset).
/// <paramref name="NextCursor"/> se envía como <c>cursor</c> para obtener la página siguiente; es null en la última.
/// </summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, long? NextCursor)
{
    public bool HasMore => NextCursor.HasValue;
}
