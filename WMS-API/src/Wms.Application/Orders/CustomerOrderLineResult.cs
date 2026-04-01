using Wms.Application.Common.Models;

namespace Wms.Application.Orders;

public sealed record CustomerOrderLineResult(
    Guid ProductId,
    int Quantity,
    MoneyModel UnitPrice);
