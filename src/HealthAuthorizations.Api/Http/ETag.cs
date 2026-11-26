using System.Globalization;

namespace HealthAuthorizations.Api.Http;

/// <summary>
/// Representa la versión de un recurso como ETag fuerte (<c>"123"</c>), derivada del token de
/// concurrencia de la base de datos.
/// </summary>
internal static class ETag
{
    public static string From(uint version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    /// <summary>Acepta <c>"123"</c>, <c>W/"123"</c> o <c>123</c>.</summary>
    public static bool TryParse(string? value, out uint version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var span = value.AsSpan().Trim();
        if (span.StartsWith("W/", StringComparison.Ordinal))
        {
            span = span[2..];
        }

        return uint.TryParse(span.Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }
}
