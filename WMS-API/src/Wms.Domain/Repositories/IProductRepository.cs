using Wms.Domain.Entities;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for products.
/// </summary>
public interface IProductRepository
{
  Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

  Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

  Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Product>> ListAsync(
      Guid? supplierId = null,
      string? searchTerm = null,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Product>> ListLowStockAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default);

  Task AddAsync(Product product, CancellationToken cancellationToken = default);

  Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

  Task<bool> IsInUseAsync(Guid productId, CancellationToken cancellationToken = default);

  Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
}
