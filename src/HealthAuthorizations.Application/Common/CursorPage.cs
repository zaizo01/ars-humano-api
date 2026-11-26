namespace HealthAuthorizations.Application.Common;

/// <summary>
/// Página de resultados con paginación por cursor (keyset).
/// <paramref name="NextCursor"/> se envía como <c>cursor</c> para obtener la página siguiente; es null en la última.
/// </summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, long? NextCursor)
{
    public bool HasMore => NextCursor.HasValue;
}

public static class CursorPage
{
    /// <summary>
    /// Construye la página a partir de una consulta que pidió <c>pageSize + 1</c> filas:
    /// la fila extra solo indica que existe una página siguiente.
    /// </summary>
    public static CursorPage<T> From<T>(List<T> rows, int pageSize, Func<T, long> cursorOf)
    {
        var hasMore = rows.Count > pageSize;
        var items = hasMore ? rows.GetRange(0, pageSize) : rows;
        return new CursorPage<T>(items, hasMore ? cursorOf(items[^1]) : null);
    }
}
