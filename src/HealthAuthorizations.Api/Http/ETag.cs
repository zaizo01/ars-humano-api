using System.Globalization;

namespace HealthAuthorizations.Api.Http;

internal static class ETag
{
    public static string From(uint version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    // Acepta "123", W/"123" o 123.
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
