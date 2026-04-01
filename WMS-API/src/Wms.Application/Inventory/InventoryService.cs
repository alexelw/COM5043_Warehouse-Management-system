using Wms.Application.Abstractions;
using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Mappers;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;

namespace Wms.Application.Inventory;

public sealed class InventoryService : IInventoryService
{
  private readonly IProductRepository _productRepository;
  private readonly ISupplierRepository _supplierRepository;
  private readonly IStockMovementRepository _stockMovementRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IClock _clock;

  public InventoryService(
      IProductRepository productRepository,
      ISupplierRepository supplierRepository,
      IStockMovementRepository stockMovementRepository,
      IUnitOfWork unitOfWork,
      IClock clock)
  {
    this._productRepository = productRepository;
    this._supplierRepository = supplierRepository;
    this._stockMovementRepository = stockMovementRepository;
    this._unitOfWork = unitOfWork;
    this._clock = clock;
  }

  public async Task<ProductResult> CreateProductAsync(ProductWriteModel model, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    await GetSupplierAsync(model.SupplierId, cancellationToken);

    var skuExists = await this._productRepository.ExistsBySkuAsync(model.Sku, cancellationToken);
    if (skuExists)
    {
      throw new ConflictException($"Product SKU '{model.Sku}' already exists.");
    }

    var product = new Product(
        model.SupplierId,
        model.Sku,
        model.Name,
        model.ReorderThreshold,
        model.UnitCost.ToDomain(nameof(model.UnitCost)));

    await this._productRepository.AddAsync(product, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
    return product.ToResult();
  }

  public async Task<IReadOnlyList<ProductResult>> GetProductsAsync(
      Guid? supplierId = null,
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    var products = await this._productRepository.ListAsync(supplierId, searchTerm, cancellationToken);
    return products.Select(product => product.ToResult()).ToArray();
  }

  public async Task<ProductResult> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    var product = await GetProductEntityAsync(productId, cancellationToken);
    return product.ToResult();
  }

  public async Task<ProductResult> UpdateProductAsync(Guid productId, ProductWriteModel model, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var product = await GetProductEntityAsync(productId, cancellationToken);
    await GetSupplierAsync(model.SupplierId, cancellationToken);

    var productWithSku = await this._productRepository.GetBySkuAsync(model.Sku, cancellationToken);
    if (productWithSku is not null && productWithSku.ProductId != productId)
    {
      throw new ConflictException($"Product SKU '{model.Sku}' already exists.");
    }

    product.ChangeSku(model.Sku);
    product.ChangeName(model.Name);
    product.ChangeSupplier(model.SupplierId);
    product.SetReorderLevel(model.ReorderThreshold);
    product.ChangeUnitCost(model.UnitCost.ToDomain(nameof(model.UnitCost)));

    await this._productRepository.UpdateAsync(product, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
    return product.ToResult();
  }

  public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    _ = await GetProductEntityAsync(productId, cancellationToken);

    if (await this._productRepository.IsInUseAsync(productId, cancellationToken))
    {
      throw new ConflictException("Cannot delete product because it is referenced by existing warehouse activity.");
    }

    await this._productRepository.DeleteAsync(productId, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<StockLevelResult>> GetStockLevelsAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    var products = await this._productRepository.ListAsync(searchTerm: searchTerm, cancellationToken: cancellationToken);
    return products.Select(product => product.ToStockLevelResult()).ToArray();
  }

  public async Task<IReadOnlyList<ProductResult>> GetLowStockProductsAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    var products = await this._productRepository.ListLowStockAsync(searchTerm, cancellationToken);
    return products.Select(product => product.ToResult()).ToArray();
  }

  public async Task<StockLevelResult> AdjustStockAsync(
      Guid productId,
      AdjustStockRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    if (string.IsNullOrWhiteSpace(request.Reason))
    {
      throw new ValidationException("Adjustment reason is required.");
    }

    var product = await GetProductEntityAsync(productId, cancellationToken);
    product.AdjustStock(request.Quantity);

    var stockMovement = StockMovement.CreateAdjustment(
        productId,
        request.Quantity,
        request.Reason,
        Guid.NewGuid(),
        this._clock.UtcNow);

    await this._productRepository.UpdateAsync(product, cancellationToken);
    await this._stockMovementRepository.AddAsync(stockMovement, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
    return product.ToStockLevelResult();
  }

  private async Task<Product> GetProductEntityAsync(Guid productId, CancellationToken cancellationToken)
  {
    var product = await this._productRepository.GetByIdAsync(productId, cancellationToken);
    if (product is null)
    {
      throw new NotFoundException(nameof(Product), productId);
    }

    return product;
  }

  private async Task GetSupplierAsync(Guid supplierId, CancellationToken cancellationToken)
  {
    var supplier = await this._supplierRepository.GetByIdAsync(supplierId, cancellationToken);
    if (supplier is null)
    {
      throw new NotFoundException(nameof(Supplier), supplierId);
    }
  }
}
