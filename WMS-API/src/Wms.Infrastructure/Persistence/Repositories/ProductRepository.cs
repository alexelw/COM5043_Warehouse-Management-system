using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Repositories;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
  private readonly WmsDbContext _dbContext;

  public ProductRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    return _dbContext.Products.SingleOrDefaultAsync(
        product => product.ProductId == productId,
        cancellationToken);
  }

  public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
  {
    var normalizedSku = sku.Trim();
    return _dbContext.Products.AsNoTracking().SingleOrDefaultAsync(
        product => product.Sku == normalizedSku,
        cancellationToken);
  }

  public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
  {
    var normalizedSku = sku.Trim();
    return _dbContext.Products.AnyAsync(product => product.Sku == normalizedSku, cancellationToken);
  }

  public async Task<IReadOnlyList<Product>> ListAsync(
      Guid? supplierId = null,
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<Product> query = _dbContext.Products.AsNoTracking();

    if (supplierId.HasValue)
    {
      query = query.Where(product => product.SupplierId == supplierId.Value);
    }

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var pattern = $"%{searchTerm.Trim()}%";
      query = query.Where(product =>
          EF.Functions.Like(product.Sku, pattern) ||
          EF.Functions.Like(product.Name, pattern));
    }

    return await query
        .OrderBy(product => product.Sku)
        .ToArrayAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<Product>> ListLowStockAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<Product> query = _dbContext.Products.AsNoTracking()
        .Where(product => product.QuantityOnHand <= product.ReorderLevel);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var pattern = $"%{searchTerm.Trim()}%";
      query = query.Where(product =>
          EF.Functions.Like(product.Sku, pattern) ||
          EF.Functions.Like(product.Name, pattern));
    }

    return await query
        .OrderBy(product => product.QuantityOnHand)
        .ThenBy(product => product.Sku)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(Product product, CancellationToken cancellationToken = default)
  {
    return _dbContext.Products.AddAsync(product, cancellationToken).AsTask();
  }

  public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
  {
    AttachIfDetached(product);
    return Task.CompletedTask;
  }

  public async Task<bool> IsInUseAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    if (await _dbContext.PurchaseOrderLines.AnyAsync(line => line.ProductId == productId, cancellationToken))
    {
      return true;
    }

    if (await _dbContext.CustomerOrderLines.AnyAsync(line => line.ProductId == productId, cancellationToken))
    {
      return true;
    }

    if (await _dbContext.GoodsReceiptLines.AnyAsync(line => line.ProductId == productId, cancellationToken))
    {
      return true;
    }

    return await _dbContext.StockMovements.AnyAsync(movement => movement.ProductId == productId, cancellationToken);
  }

  public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    var product = await GetByIdAsync(productId, cancellationToken);
    if (product is not null)
    {
      _dbContext.Products.Remove(product);
    }
  }

  private void AttachIfDetached(Product product)
  {
    if (_dbContext.Entry(product).State == EntityState.Detached)
    {
      _dbContext.Products.Attach(product);
    }
  }
}
