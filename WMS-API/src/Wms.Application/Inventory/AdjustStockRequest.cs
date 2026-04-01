namespace Wms.Application.Inventory;

public sealed record AdjustStockRequest(int Quantity, string Reason);
