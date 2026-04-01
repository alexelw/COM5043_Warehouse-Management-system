namespace Wms.Contracts.PurchaseOrders;

public sealed record ReceiveDeliveryRequest : IValidatableObject
{
  [Required]
  [MinLength(1)]
  public List<GoodsReceiptLineRequest> Lines { get; init; } = [];

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.Lines.Count == 0)
    {
      yield return new ValidationResult(
          "At least one line is required.",
          new[] { nameof(this.Lines) });
    }
  }
}
