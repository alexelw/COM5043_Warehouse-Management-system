namespace Wms.Contracts.PurchaseOrders;

public sealed record GoodsReceiptLineRequest : IValidatableObject
{
  [Required]
  public Guid ProductId { get; init; }

  [Range(1, int.MaxValue)]
  public int QuantityReceived { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.ProductId == Guid.Empty)
    {
      yield return new ValidationResult(
          "ProductId is required.",
          new[] { nameof(this.ProductId) });
    }
  }
}
