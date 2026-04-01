namespace Wms.Application.Orders;

public sealed record CreateCustomerOrderRequest(
    CustomerInputModel Customer,
    IReadOnlyList<CustomerOrderLineInput> Lines);
