namespace Wms.Contracts.Common;

public sealed record MoneyDto : IValidatableObject
{
  public decimal Amount { get; init; }

  [Required]
  public string Currency { get; init; } = "GBP";

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (!string.Equals(this.Currency?.Trim(), "GBP", StringComparison.OrdinalIgnoreCase))
    {
      yield return new ValidationResult(
          "Currency must be GBP.",
          new[] { nameof(this.Currency) });
    }
  }
}
