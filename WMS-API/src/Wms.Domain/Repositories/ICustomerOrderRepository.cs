using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for customer orders.
/// </summary>
public interface ICustomerOrderRepository
{
  Task<CustomerOrder?> GetByIdAsync(Guid customerOrderId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<CustomerOrder>> ListAsync(
      Guid? customerId = null,
      CustomerOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default);

  Task AddAsync(CustomerOrder customerOrder, CancellationToken cancellationToken = default);

  Task UpdateAsync(CustomerOrder customerOrder, CancellationToken cancellationToken = default);
}
