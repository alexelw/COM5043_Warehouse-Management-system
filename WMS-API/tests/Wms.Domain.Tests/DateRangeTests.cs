using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class DateRangeTests
{
  [Fact]
  public void Constructor_WhenEndDateIsBeforeStartDate_ThrowsDomainRuleViolationException()
  {
    var from = new DateTime(2026, 1, 2);
    var to = new DateTime(2026, 1, 1);

    var action = () => new DateRange(from, to);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Contains_WhenValueIsAtBoundary_ReturnsTrue()
  {
    var range = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

    Assert.True(range.Contains(new DateTime(2026, 1, 1)));
    Assert.True(range.Contains(new DateTime(2026, 1, 31)));
  }

  [Fact]
  public void Overlaps_WhenRangesAreDisjoint_ReturnsFalse()
  {
    var left = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
    var right = new DateRange(new DateTime(2026, 1, 11), new DateTime(2026, 1, 20));

    var result = left.Overlaps(right);

    Assert.False(result);
  }
}
