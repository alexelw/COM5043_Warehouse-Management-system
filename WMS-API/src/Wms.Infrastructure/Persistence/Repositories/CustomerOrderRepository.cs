using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class CustomerOrderRepository : ICustomerOrderRepository
{
  private readonly WmsDbContext _dbContext;

  public CustomerOrderRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<CustomerOrder?> GetByIdAsync(Guid customerOrderId, CancellationToken cancellationToken = default)
  {
    return _dbContext.CustomerOrders
        .Include(customerOrder => customerOrder.Lines)
        .SingleOrDefaultAsync(
            customerOrder => customerOrder.CustomerOrderId == customerOrderId,
            cancellationToken);
  }

  public async Task<IReadOnlyList<CustomerOrder>> ListAsync(
      Guid? customerId = null,
      CustomerOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<CustomerOrder> query = _dbContext.CustomerOrders
        .AsNoTracking()
        .Include(customerOrder => customerOrder.Lines);

    if (customerId.HasValue)
    {
      query = query.Where(customerOrder => customerOrder.CustomerId == customerId.Value);
    }

    if (status.HasValue)
    {
      query = query.Where(customerOrder => customerOrder.Status == status.Value);
    }

    if (createdWithin is not null)
    {
      query = query.Where(customerOrder =>
          customerOrder.CreatedAt >= createdWithin.From &&
          customerOrder.CreatedAt <= createdWithin.To);
    }

    return await query
        .OrderByDescending(customerOrder => customerOrder.CreatedAt)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(CustomerOrder customerOrder, CancellationToken cancellationToken = default)
  {
    return _dbContext.CustomerOrders.AddAsync(customerOrder, cancellationToken).AsTask();
  }

  public Task UpdateAsync(CustomerOrder customerOrder, CancellationToken cancellationToken = default)
  {
    AttachIfDetached(customerOrder);
    return Task.CompletedTask;
  }

  private void AttachIfDetached(CustomerOrder customerOrder)
  {
    if (_dbContext.Entry(customerOrder).State == EntityState.Detached)
    {
      _dbContext.CustomerOrders.Attach(customerOrder);
    }
  }
}
