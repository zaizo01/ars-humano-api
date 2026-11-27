namespace HealthAuthorizations.Application.Common;

public sealed record CursorPage<T>(IReadOnlyList<T> Items, long? NextCursor)
{
    public bool HasMore => NextCursor.HasValue;
}

public static class CursorPage
{
    // rows trae pageSize + 1: la fila extra solo indica que hay otra página.
    public static CursorPage<T> From<T>(List<T> rows, int pageSize, Func<T, long> cursorOf)
    {
        var hasMore = rows.Count > pageSize;
        var items = hasMore ? rows.GetRange(0, pageSize) : rows;
        return new CursorPage<T>(items, hasMore ? cursorOf(items[^1]) : null);
    }
}
