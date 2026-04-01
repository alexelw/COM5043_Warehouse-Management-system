namespace Wms.Contracts.Finance;

using Wms.Contracts.Common;

public sealed record VoidOrReverseTransactionRequest : IValidatableObject
{
  [Required]
  [OpenApiAllowedValues("Void", "Reverse")]
  public string Action { get; init; } = string.Empty;

  public string? Reason { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (!string.Equals(this.Action, "Void", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(this.Action, "Reverse", StringComparison.OrdinalIgnoreCase))
    {
      yield return new ValidationResult(
          "Action must be Void or Reverse.",
          new[] { nameof(this.Action) });
    }

    if (string.Equals(this.Action, "Void", StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrWhiteSpace(this.Reason))
    {
      yield return new ValidationResult(
          "Reason is required when action is Void.",
          new[] { nameof(this.Reason) });
    }
  }
}
