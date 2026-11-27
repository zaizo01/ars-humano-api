namespace HealthAuthorizations.Application.Common;

public sealed class CoverageOptions
{
    public const string SectionName = "Coverage";

    public decimal CoveragePercentage { get; init; } = 70m;
}
