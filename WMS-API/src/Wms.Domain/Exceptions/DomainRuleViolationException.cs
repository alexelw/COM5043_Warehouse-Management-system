namespace Wms.Domain.Exceptions;

/// <summary>
/// Raised when a domain rule or invariant is broken.
/// </summary>
public class DomainRuleViolationException : Exception
{
  public DomainRuleViolationException()
  {
  }

  public DomainRuleViolationException(string message)
      : base(message)
  {
  }

  public DomainRuleViolationException(string message, Exception innerException)
      : base(message, innerException)
  {
  }
}
