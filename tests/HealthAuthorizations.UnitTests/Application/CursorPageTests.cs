using HealthAuthorizations.Application.Common;

namespace HealthAuthorizations.UnitTests.Application;

[TestFixture]
public sealed class CursorPageTests
{
    [Test]
    public void Should_TrimExtraRowAndSetNextCursor_When_MoreRowsThanPageSize()
    {
        // Arrange
        List<long> rows = [10, 20, 30, 40];

        // Act
        var page = CursorPage.From(rows, pageSize: 3, x => x);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Is.EqualTo(new long[] { 10, 20, 30 }));
            Assert.That(page.NextCursor, Is.EqualTo(30));
            Assert.That(page.HasMore, Is.True);
        }
    }

    [Test]
    public void Should_ReturnAllRowsWithoutCursor_When_RowsFitInPage()
    {
        // Arrange
        List<long> rows = [10, 20, 30];

        // Act
        var page = CursorPage.From(rows, pageSize: 3, x => x);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Has.Count.EqualTo(3));
            Assert.That(page.NextCursor, Is.Null);
            Assert.That(page.HasMore, Is.False);
        }
    }

    [Test]
    public void Should_ReturnEmptyPage_When_NoRows()
    {
        // Arrange
        List<long> rows = [];

        // Act
        var page = CursorPage.From(rows, pageSize: 20, x => x);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Is.Empty);
            Assert.That(page.HasMore, Is.False);
        }
    }
}
