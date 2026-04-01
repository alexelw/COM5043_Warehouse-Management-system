using Wms.Domain.Enums;

namespace Wms.Application.Orders;

public interface IOrderService
{
  Task<CustomerOrderResult> CreateCustomerOrderAsync(
      CreateCustomerOrderRequest request,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<CustomerOrderResult>> GetCustomerOrdersAsync(
      Guid? customerId = null,
      CustomerOrderStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);

  Task<CustomerOrderResult> GetCustomerOrderAsync(
      Guid customerOrderId,
      CancellationToken cancellationToken = default);

  Task<CustomerOrderResult> CancelCustomerOrderAsync(
      Guid customerOrderId,
      CancelCustomerOrderRequest request,
      CancellationToken cancellationToken = default);
}
