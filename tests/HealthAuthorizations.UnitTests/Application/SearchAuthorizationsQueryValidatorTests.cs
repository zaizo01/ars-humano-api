using HealthAuthorizations.Application.Authorizations.Search;
using HealthAuthorizations.Domain.Authorizations;

namespace HealthAuthorizations.UnitTests.Application;

[TestFixture]
public sealed class SearchAuthorizationsQueryValidatorTests
{
    private SearchAuthorizationsQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SearchAuthorizationsQueryValidator();

    [Test]
    public void Should_BeValid_When_NoFiltersAreProvided()
    {
        // Arrange
        var query = new SearchAuthorizationsQuery();

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Should_BeValid_When_AllFiltersAreValid()
    {
        // Arrange
        var query = new SearchAuthorizationsQuery
        {
            Number = 10,
            AffiliateId = 1,
            ProviderId = 2,
            ServiceTypeId = 1,
            Status = AuthorizationStatus.Active,
            FromDate = new DateOnly(2026, 1, 1),
            ToDate = new DateOnly(2026, 12, 31),
            Cursor = 50,
            PageSize = SearchAuthorizationsQuery.MaxPageSize,
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(SearchAuthorizationsQuery.MaxPageSize + 1)]
    public void Should_FailOnPageSize_When_PageSizeIsOutOfRange(int pageSize)
    {
        // Arrange
        var query = new SearchAuthorizationsQuery { PageSize = pageSize };

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchAuthorizationsQuery.PageSize)));
    }

    [Test]
    public void Should_FailOnToDate_When_ToDateIsBeforeFromDate()
    {
        // Arrange
        var query = new SearchAuthorizationsQuery { FromDate = new DateOnly(2026, 6, 1), ToDate = new DateOnly(2026, 5, 31) };

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchAuthorizationsQuery.ToDate)));
    }

    [Test]
    public void Should_FailOnCursor_When_CursorIsNotPositive()
    {
        // Arrange
        var query = new SearchAuthorizationsQuery { Cursor = 0 };

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchAuthorizationsQuery.Cursor)));
    }

    [Test]
    public void Should_FailOnStatus_When_StatusIsUndefined()
    {
        // Arrange
        var query = new SearchAuthorizationsQuery { Status = (AuthorizationStatus)99 };

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(SearchAuthorizationsQuery.Status)));
    }
}
