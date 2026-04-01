using Wms.Domain.Entities;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for suppliers.
/// </summary>
public interface ISupplierRepository
{
  Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Supplier>> ListAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

  Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

  Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

  Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default);
}
