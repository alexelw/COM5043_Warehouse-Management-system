namespace Wms.Contracts.Orders;

public sealed record CreateCustomerOrderRequest : IValidatableObject
{
  [Required]
  public CustomerDto Customer { get; init; } = new();

  [Required]
  [MinLength(1)]
  public List<CustomerOrderLineRequest> Lines { get; init; } = [];

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
