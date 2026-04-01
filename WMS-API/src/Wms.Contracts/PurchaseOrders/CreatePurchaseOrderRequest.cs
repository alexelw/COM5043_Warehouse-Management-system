namespace Wms.Contracts.PurchaseOrders;

public sealed record CreatePurchaseOrderRequest : IValidatableObject
{
  [Required]
  public Guid SupplierId { get; init; }

  [Required]
  [MinLength(1)]
  public List<PurchaseOrderLineRequest> Lines { get; init; } = [];

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.SupplierId == Guid.Empty)
    {
      yield return new ValidationResult(
          "SupplierId is required.",
          new[] { nameof(this.SupplierId) });
    }

    if (this.Lines.Count == 0)
    {
      yield return new ValidationResult(
          "At least one line is required.",
          new[] { nameof(this.Lines) });
    }
  }
}
