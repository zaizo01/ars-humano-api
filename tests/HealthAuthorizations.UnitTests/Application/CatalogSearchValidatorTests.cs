using HealthAuthorizations.Application.Catalogs;
using HealthAuthorizations.Application.Common;

namespace HealthAuthorizations.UnitTests.Application;

[TestFixture]
public sealed class CatalogSearchValidatorTests
{
    [Test]
    public void Should_BeValid_When_AffiliateSearchHasDefaults()
    {
        // Arrange
        var validator = new SearchAffiliatesQueryValidator();

        // Act
        var result = validator.Validate(new SearchAffiliatesQuery());

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(0)]
    [TestCase(PaginationRules.MaxPageSize + 1)]
    public void Should_FailOnPageSize_When_AffiliatePageSizeIsOutOfRange(int pageSize)
    {
        // Arrange
        var validator = new SearchAffiliatesQueryValidator();

        // Act
        var result = validator.Validate(new SearchAffiliatesQuery { PageSize = pageSize });

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchAffiliatesQuery.PageSize)));
    }

    [Test]
    public void Should_FailOnSearch_When_AffiliateSearchTermIsTooLong()
    {
        // Arrange
        var validator = new SearchAffiliatesQueryValidator();

        // Act
        var result = validator.Validate(new SearchAffiliatesQuery { Search = new string('a', 101) });

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchAffiliatesQuery.Search)));
    }

    [Test]
    public void Should_BeValid_When_ProviderSearchHasDefaults()
    {
        // Arrange
        var validator = new SearchProvidersQueryValidator();

        // Act
        var result = validator.Validate(new SearchProvidersQuery());

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Should_FailOnCursor_When_ProviderCursorIsNegative()
    {
        // Arrange
        var validator = new SearchProvidersQueryValidator();

        // Act
        var result = validator.Validate(new SearchProvidersQuery { Cursor = -5 });

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchProvidersQuery.Cursor)));
    }
}
