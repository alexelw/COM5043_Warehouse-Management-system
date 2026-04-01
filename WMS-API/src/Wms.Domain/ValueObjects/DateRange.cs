using Wms.Domain.Exceptions;

namespace Wms.Domain.ValueObjects;

/// <summary>
/// Represents an inclusive date range used in filters and reports.
/// </summary>
public sealed record DateRange
{
  private DateRange()
  {
  }

  public DateRange(DateTime from, DateTime to)
  {
    if (to < from)
    {
      throw new DomainRuleViolationException("Date range end must be on or after the start date.");
    }

    this.From = from;
    this.To = to;
  }

  public DateTime From { get; init; }

  public DateTime To { get; init; }

  public bool Contains(DateTime value) => value >= this.From && value <= this.To;

  public bool Overlaps(DateRange other)
  {
    ArgumentNullException.ThrowIfNull(other);
    return this.From <= other.To && other.From <= this.To;
  }

  public override string ToString() => $"{this.From:yyyy-MM-dd} to {this.To:yyyy-MM-dd}";
}
