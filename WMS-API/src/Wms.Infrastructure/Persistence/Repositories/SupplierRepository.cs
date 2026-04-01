using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Repositories;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class SupplierRepository : ISupplierRepository
{
  private readonly WmsDbContext _dbContext;

  public SupplierRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    return _dbContext.Suppliers.SingleOrDefaultAsync(
        supplier => supplier.SupplierId == supplierId,
        cancellationToken);
  }

  public async Task<IReadOnlyList<Supplier>> ListAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
  {
    IQueryable<Supplier> query = _dbContext.Suppliers.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var pattern = $"%{searchTerm.Trim()}%";
      query = query.Where(supplier =>
          EF.Functions.Like(supplier.Name, pattern) ||
          EF.Functions.Like(supplier.Contact.Email ?? string.Empty, pattern) ||
          EF.Functions.Like(supplier.Contact.Phone ?? string.Empty, pattern));
    }

    return await query
        .OrderBy(supplier => supplier.Name)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
  {
    return _dbContext.Suppliers.AddAsync(supplier, cancellationToken).AsTask();
  }

  public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
  {
    AttachIfDetached(supplier);
    return Task.CompletedTask;
  }

  public async Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    var supplier = await GetByIdAsync(supplierId, cancellationToken);
    if (supplier is not null)
    {
      _dbContext.Suppliers.Remove(supplier);
    }
  }

  private void AttachIfDetached(Supplier supplier)
  {
    if (_dbContext.Entry(supplier).State == EntityState.Detached)
    {
      _dbContext.Suppliers.Attach(supplier);
    }
  }
}
