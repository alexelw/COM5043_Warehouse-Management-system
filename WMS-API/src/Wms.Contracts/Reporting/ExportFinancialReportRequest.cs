namespace Wms.Contracts.Reporting;

using Wms.Contracts.Common;

public sealed record ExportFinancialReportRequest : IValidatableObject
{
  [Required]
  [OpenApiAllowedValues("TXT", "JSON")]
  public string Format { get; init; } = string.Empty;

  public DateOnly? From { get; init; }

  public DateOnly? To { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (!string.Equals(this.Format, "TXT", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(this.Format, "JSON", StringComparison.OrdinalIgnoreCase))
    {
      yield return new ValidationResult(
          "Format must be TXT or JSON.",
          new[] { nameof(this.Format) });
    }

    if (this.From.HasValue && this.To.HasValue && this.From.Value > this.To.Value)
    {
      yield return new ValidationResult(
          "From date must be on or before To date.",
          new[] { nameof(this.From), nameof(this.To) });
    }
  }
}
