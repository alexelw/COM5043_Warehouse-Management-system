namespace Wms.Domain.Exceptions;

/// <summary>
/// Raised when an operation would take stock below zero.
/// </summary>
public class InsufficientStockException : DomainRuleViolationException
{
  public InsufficientStockException(string productIdentifier, int requestedQuantity, int availableQuantity)
      : this(
          productIdentifier,
          requestedQuantity,
          availableQuantity,
          $"Insufficient stock for {productIdentifier}. Requested {requestedQuantity}, available {availableQuantity}.")
  {
  }

  public InsufficientStockException(
      string productIdentifier,
      int requestedQuantity,
      int availableQuantity,
      string message)
      : base(message)
  {
    this.ProductIdentifier = productIdentifier;
    this.RequestedQuantity = requestedQuantity;
    this.AvailableQuantity = availableQuantity;
  }

  public InsufficientStockException(string message)
      : base(message)
  {
  }

  public InsufficientStockException(string message, Exception innerException)
      : base(message, innerException)
  {
  }

  public string? ProductIdentifier { get; }

  public int? RequestedQuantity { get; }

  public int? AvailableQuantity { get; }
}
