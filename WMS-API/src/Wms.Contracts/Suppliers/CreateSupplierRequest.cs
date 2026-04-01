namespace Wms.Contracts.Suppliers;

public sealed record CreateSupplierRequest : IValidatableObject
{
  [Required]
  public string Name { get; init; } = string.Empty;

  [EmailAddress]
  public string? Email { get; init; }

  [Phone]
  public string? Phone { get; init; }

  public string? Address { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (string.IsNullOrWhiteSpace(this.Email) &&
        string.IsNullOrWhiteSpace(this.Phone) &&
        string.IsNullOrWhiteSpace(this.Address))
    {
      yield return new ValidationResult(
          "At least one contact field must be provided.",
          new[] { nameof(this.Email), nameof(this.Phone), nameof(this.Address) });
    }
  }
}
