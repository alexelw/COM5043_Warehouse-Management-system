namespace Wms.Application.Orders;

public sealed record CustomerInputModel(
    string Name,
    string? Email,
    string? Phone);
