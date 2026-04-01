using Wms.Domain.Exceptions;

namespace Wms.Domain.Entities;

public abstract class LineCollectionDocument<TLine>
    where TLine : class
{
  public IReadOnlyCollection<TLine> Lines => this.MutableLines.AsReadOnly();

  protected abstract List<TLine> MutableLines { get; }

  protected void AddLines(IEnumerable<TLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);

    foreach (var line in lines)
    {
      this.AddLine(line);
    }
  }

  protected void AddLine(TLine line)
  {
    ArgumentNullException.ThrowIfNull(line);

    this.PrepareLineForAdd(line);
    this.MutableLines.Add(line);
  }

  protected void EnsureHasLines(string message)
  {
    if (this.MutableLines.Count == 0)
    {
      throw new DomainRuleViolationException(message);
    }
  }

  protected abstract void PrepareLineForAdd(TLine line);
}
