namespace Wms.Application.Suppliers;

public sealed record SupplierWriteModel(
    string Name,
    string? Email,
    string? Phone,
    string? Address);
