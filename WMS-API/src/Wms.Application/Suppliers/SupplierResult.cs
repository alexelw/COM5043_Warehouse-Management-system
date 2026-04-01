namespace Wms.Application.Suppliers;

public sealed record SupplierResult(
    Guid SupplierId,
    string Name,
    string? Email,
    string? Phone,
    string? Address);
