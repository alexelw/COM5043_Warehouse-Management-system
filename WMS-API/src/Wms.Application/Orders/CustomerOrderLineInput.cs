using Wms.Application.Common.Models;

namespace Wms.Application.Orders;

public sealed record CustomerOrderLineInput(
    Guid ProductId,
    int Quantity,
    MoneyModel UnitPrice);
