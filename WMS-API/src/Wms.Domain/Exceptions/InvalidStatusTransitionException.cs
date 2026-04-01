namespace Wms.Domain.Exceptions;

/// <summary>
/// Raised when an aggregate is pushed into a status change it does not allow.
/// </summary>
public class InvalidStatusTransitionException : DomainRuleViolationException
{
  public InvalidStatusTransitionException(string aggregateName, string currentStatus, string targetStatus)
      : this(
          aggregateName,
          currentStatus,
          targetStatus,
          $"Invalid status transition for {aggregateName}: {currentStatus} -> {targetStatus}.")
  {
  }

  public InvalidStatusTransitionException(
      string aggregateName,
      string currentStatus,
      string targetStatus,
      string message)
      : base(message)
  {
    this.AggregateName = aggregateName;
    this.CurrentStatus = currentStatus;
    this.TargetStatus = targetStatus;
  }

  public InvalidStatusTransitionException(string message)
      : base(message)
  {
  }

  public InvalidStatusTransitionException(string message, Exception innerException)
      : base(message, innerException)
  {
  }

  public string? AggregateName { get; }

  public string? CurrentStatus { get; }

  public string? TargetStatus { get; }
}
