using Wms.Application.Common.Models;
using Wms.Domain.Enums;

namespace Wms.Application.Orders;

public sealed record CustomerOrderResult(
    Guid CustomerOrderId,
    Guid CustomerId,
    CustomerOrderStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<CustomerOrderLineResult> Lines,
    MoneyModel TotalAmount);
