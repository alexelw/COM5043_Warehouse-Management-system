using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Repositories;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
  private readonly WmsDbContext _dbContext;

  public CustomerRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
  {
    return _dbContext.Customers.SingleOrDefaultAsync(
        customer => customer.CustomerId == customerId,
        cancellationToken);
  }

  public async Task<IReadOnlyList<Customer>> ListAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
  {
    IQueryable<Customer> query = _dbContext.Customers.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var pattern = $"%{searchTerm.Trim()}%";
      query = query.Where(customer => EF.Functions.Like(customer.Name, pattern));
    }

    return await query
        .OrderBy(customer => customer.Name)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
  {
    return _dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();
  }

  public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
  {
    AttachIfDetached(customer);
    return Task.CompletedTask;
  }

  private void AttachIfDetached(Customer customer)
  {
    if (_dbContext.Entry(customer).State == EntityState.Detached)
    {
      _dbContext.Customers.Attach(customer);
    }
  }
}
