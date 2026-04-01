using Wms.Api.Infrastructure;

namespace Wms.Application.Tests;

public class ApiEndpointHelpersTests
{
  [Fact]
  public void ApplyListOptions_WhenSortingAndPagingApplied_ReturnsExpectedPage()
  {
    var items = new[]
    {
            new SortableItem("Bravo", 2),
            new SortableItem("Alpha", 3),
            new SortableItem("Charlie", 1),
        };

    var result = ApiEndpointHelpers.ApplyListOptions(
        items,
        sort: "name",
        order: "asc",
        page: 2,
        pageSize: 1,
        defaultSort: "rank",
        defaultDescending: true,
        new Dictionary<string, Func<SortableItem, IComparable?>>
        {
          ["name"] = item => item.Name,
          ["rank"] = item => item.Rank,
        });

    var item = Assert.Single(result);
    Assert.Equal("Bravo", item.Name);
  }

  [Fact]
  public void ApplyListOptions_WhenSortFieldInvalid_ThrowsValidationException()
  {
    var items = new[] { new SortableItem("Alpha", 1) };

    var exception = Assert.Throws<RequestValidationException>(() => ApiEndpointHelpers.ApplyListOptions(
        items,
        sort: "unknown",
        order: "asc",
        page: 1,
        pageSize: 50,
        defaultSort: "name",
        defaultDescending: false,
        new Dictionary<string, Func<SortableItem, IComparable?>>
        {
          ["name"] = item => item.Name,
        }));

    Assert.Equal("Sort must be one of: name.", exception.Errors["sort"].Single());
  }

  [Fact]
  public void ParseOptionalEnum_WhenValueInvalid_ThrowsValidationException()
  {
    var exception = Assert.Throws<RequestValidationException>(() =>
        ApiEndpointHelpers.ParseOptionalEnum<DayOfWeek>("noday", "status"));

    Assert.Equal(
        "status must be one of: Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday.",
        exception.Errors["status"].Single());
  }

  [Fact]
  public void ParseOptionalDate_WhenValueInvalid_ThrowsValidationException()
  {
    var exception = Assert.Throws<RequestValidationException>(() =>
        ApiEndpointHelpers.ParseOptionalDate("2026-99-99", "from"));

    Assert.Equal("from must be a valid date in YYYY-MM-DD format.", exception.Errors["from"].Single());
  }

  [Fact]
  public void ParseOptionalDate_WhenValueNotIso8601_ThrowsValidationException()
  {
    var exception = Assert.Throws<RequestValidationException>(() =>
        ApiEndpointHelpers.ParseOptionalDate("18/03/2026", "from"));

    Assert.Equal("from must be a valid date in YYYY-MM-DD format.", exception.Errors["from"].Single());
  }

  [Fact]
  public void ValidateDateRange_WhenFromAfterTo_ThrowsValidationException()
  {
    var from = new DateOnly(2026, 3, 19);
    var to = new DateOnly(2026, 3, 18);

    var exception = Assert.Throws<RequestValidationException>(() =>
        ApiEndpointHelpers.ValidateDateRange(from, to));

    Assert.Equal("From date must be on or before To date.", exception.Errors["from"].Single());
    Assert.Equal("From date must be on or before To date.", exception.Errors["to"].Single());
  }

  private sealed record SortableItem(string Name, int Rank);
}
