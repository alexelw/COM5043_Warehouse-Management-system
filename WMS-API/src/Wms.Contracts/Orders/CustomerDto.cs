namespace Wms.Contracts.Orders;

public sealed record CustomerDto
{
  [Required]
  public string Name { get; init; } = string.Empty;

  [EmailAddress]
  public string? Email { get; init; }

  [Phone]
  public string? Phone { get; init; }
}
