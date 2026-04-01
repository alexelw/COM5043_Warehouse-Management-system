namespace Wms.Application.Inventory;

public interface IInventoryService
{
  Task<ProductResult> CreateProductAsync(ProductWriteModel model, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ProductResult>> GetProductsAsync(
      Guid? supplierId = null,
      string? searchTerm = null,
      CancellationToken cancellationToken = default);

  Task<ProductResult> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);

  Task<ProductResult> UpdateProductAsync(Guid productId, ProductWriteModel model, CancellationToken cancellationToken = default);

  Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StockLevelResult>> GetStockLevelsAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ProductResult>> GetLowStockProductsAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default);

  Task<StockLevelResult> AdjustStockAsync(
      Guid productId,
      AdjustStockRequest request,
      CancellationToken cancellationToken = default);
}
