namespace Wms.Contracts.Suppliers;

public sealed record SupplierResponse
{
  public Guid SupplierId { get; init; }

  public string Name { get; init; } = string.Empty;

  public string? Email { get; init; }

  public string? Phone { get; init; }

  public string? Address { get; init; }
}
