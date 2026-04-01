using Wms.Application.Abstractions;
using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Mappers;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Application.PurchaseOrders;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
  private readonly IPurchaseOrderRepository _purchaseOrderRepository;
  private readonly ISupplierRepository _supplierRepository;
  private readonly IProductRepository _productRepository;
  private readonly IGoodsReceiptRepository _goodsReceiptRepository;
  private readonly IStockMovementRepository _stockMovementRepository;
  private readonly ITransactionRepository _transactionRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IClock _clock;

  public PurchaseOrderService(
      IPurchaseOrderRepository purchaseOrderRepository,
      ISupplierRepository supplierRepository,
      IProductRepository productRepository,
      IGoodsReceiptRepository goodsReceiptRepository,
      IStockMovementRepository stockMovementRepository,
      ITransactionRepository transactionRepository,
      IUnitOfWork unitOfWork,
      IClock clock)
  {
    _purchaseOrderRepository = purchaseOrderRepository;
    _supplierRepository = supplierRepository;
    _productRepository = productRepository;
    _goodsReceiptRepository = goodsReceiptRepository;
    _stockMovementRepository = stockMovementRepository;
    _transactionRepository = transactionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
  }

  public async Task<PurchaseOrderResult> CreatePurchaseOrderAsync(
      CreatePurchaseOrderRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(request.Lines);

    await EnsureSupplierExistsAsync(request.SupplierId, cancellationToken);

    var products = await LoadProductsAsync(
        request.Lines.Select(line => line.ProductId),
        cancellationToken);

    var lines = request.Lines
        .Select(line => new PurchaseOrderLine(
            line.ProductId,
            line.Quantity,
            line.UnitCost.ToDomain(nameof(line.UnitCost))))
        .ToArray();

    foreach (var line in lines)
    {
      var product = products[line.ProductId];
      if (product.SupplierId != request.SupplierId)
      {
        throw new ValidationException("All purchase order lines must belong to the selected supplier.");
      }
    }

    var createdAt = _clock.UtcNow;

    var purchaseOrder = new PurchaseOrder(request.SupplierId, lines, createdAt);
    var pendingExpense = new FinancialTransaction(
        FinancialTransactionType.PurchaseExpense,
        purchaseOrder.TotalOrderedAmount,
        ReferenceType.PurchaseOrder,
        purchaseOrder.PurchaseOrderId,
        createdAt);

    await _purchaseOrderRepository.AddAsync(purchaseOrder, cancellationToken);
    await _transactionRepository.AddAsync(pendingExpense, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return purchaseOrder.ToResult();
  }

  public async Task<IReadOnlyList<PurchaseOrderResult>> GetPurchaseOrdersAsync(
      Guid? supplierId = null,
      PurchaseOrderStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    var purchaseOrders = await _purchaseOrderRepository.ListAsync(
        supplierId,
        status,
        ApplicationMapping.ToDateRange(from, to),
        cancellationToken);

    return purchaseOrders.Select(purchaseOrder => purchaseOrder.ToResult()).ToArray();
  }

  public async Task<PurchaseOrderResult> GetPurchaseOrderAsync(
      Guid purchaseOrderId,
      CancellationToken cancellationToken = default)
  {
    var purchaseOrder = await GetPurchaseOrderEntityAsync(purchaseOrderId, cancellationToken);
    return purchaseOrder.ToResult();
  }

  public async Task<PurchaseOrderResult> CancelPurchaseOrderAsync(
      Guid purchaseOrderId,
      CancelPurchaseOrderRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    EnsureReason(request.Reason, "Cancellation reason is required.");

    var purchaseOrder = await GetPurchaseOrderEntityAsync(purchaseOrderId, cancellationToken);
    purchaseOrder.Cancel();

    await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
    await VoidPendingPurchaseTransactionsAsync(purchaseOrder.PurchaseOrderId, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return purchaseOrder.ToResult();
  }

  public async Task<GoodsReceiptResult> ReceiveDeliveryAsync(
      Guid purchaseOrderId,
      ReceiveDeliveryRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(request.Lines);

    var purchaseOrder = await GetPurchaseOrderEntityAsync(purchaseOrderId, cancellationToken);
    var products = await LoadProductsAsync(
        request.Lines.Select(line => line.ProductId),
        cancellationToken);

    var purchaseOrderLines = purchaseOrder.Lines.ToDictionary(line => line.ProductId);
    var receivedAt = _clock.UtcNow;
    var receiptLines = request.Lines
        .Select(line => new GoodsReceiptLine(line.ProductId, line.QuantityReceived))
        .ToArray();

    var goodsReceipt = new GoodsReceipt(purchaseOrderId, receiptLines, receivedAt);
    purchaseOrder.ReceiveGoods(goodsReceipt);

    var stockMovements = new List<StockMovement>(goodsReceipt.Lines.Count);
    var receivedAmount = Money.Zero;

    foreach (var line in goodsReceipt.Lines)
    {
      if (!purchaseOrderLines.TryGetValue(line.ProductId, out var purchaseOrderLine))
      {
        throw new ValidationException("Received product is not part of the purchase order.");
      }

      var product = products[line.ProductId];
      product.IncreaseStock(line.QuantityReceived);
      product.ChangeUnitCost(purchaseOrderLine.UnitCostAtOrder);

      receivedAmount += purchaseOrderLine.UnitCostAtOrder * line.QuantityReceived;
      stockMovements.Add(StockMovement.CreateReceipt(
          line.ProductId,
          line.QuantityReceived,
          ReferenceType.GoodsReceipt,
          goodsReceipt.GoodsReceiptId,
          receivedAt));
    }

    var postedExpense = new FinancialTransaction(
        FinancialTransactionType.PurchaseExpense,
        receivedAmount,
        ReferenceType.PurchaseOrder,
        purchaseOrder.PurchaseOrderId,
        receivedAt);
    postedExpense.MarkPosted();

    await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
    await _goodsReceiptRepository.AddAsync(goodsReceipt, cancellationToken);

    foreach (var product in products.Values)
    {
      await _productRepository.UpdateAsync(product, cancellationToken);
    }

    await _stockMovementRepository.AddRangeAsync(stockMovements, cancellationToken);
    await _transactionRepository.AddAsync(postedExpense, cancellationToken);
    await RefreshPendingPurchaseExpenseAsync(purchaseOrder, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return goodsReceipt.ToResult();
  }

  public async Task<IReadOnlyList<GoodsReceiptResult>> GetReceiptsAsync(
      Guid purchaseOrderId,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    _ = await GetPurchaseOrderEntityAsync(purchaseOrderId, cancellationToken);

    var receipts = await _goodsReceiptRepository.ListByPurchaseOrderAsync(
        purchaseOrderId,
        ApplicationMapping.ToDateRange(from, to),
        cancellationToken);

    return receipts.Select(receipt => receipt.ToResult()).ToArray();
  }

  private async Task<PurchaseOrder> GetPurchaseOrderEntityAsync(
      Guid purchaseOrderId,
      CancellationToken cancellationToken)
  {
    var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, cancellationToken);
    if (purchaseOrder is null)
    {
      throw new NotFoundException(nameof(PurchaseOrder), purchaseOrderId);
    }

    return purchaseOrder;
  }

  private async Task EnsureSupplierExistsAsync(Guid supplierId, CancellationToken cancellationToken)
  {
    var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
    if (supplier is null)
    {
      throw new NotFoundException(nameof(Supplier), supplierId);
    }
  }

  private async Task<Dictionary<Guid, Product>> LoadProductsAsync(
      IEnumerable<Guid> productIds,
      CancellationToken cancellationToken)
  {
    var products = new Dictionary<Guid, Product>();

    foreach (var productId in productIds.Distinct())
    {
      var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
      if (product is null)
      {
        throw new NotFoundException(nameof(Product), productId);
      }

      products[productId] = product;
    }

    return products;
  }

  private async Task VoidPendingPurchaseTransactionsAsync(
      Guid purchaseOrderId,
      CancellationToken cancellationToken)
  {
    var transactions = await _transactionRepository.ListByReferenceAsync(
        ReferenceType.PurchaseOrder,
        purchaseOrderId,
        cancellationToken);

    foreach (var transaction in transactions.Where(static transaction => transaction.Status == FinancialTransactionStatus.Pending))
    {
      transaction.MarkVoided();
      await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
  }

  private async Task RefreshPendingPurchaseExpenseAsync(
      PurchaseOrder purchaseOrder,
      CancellationToken cancellationToken)
  {
    await VoidPendingPurchaseTransactionsAsync(purchaseOrder.PurchaseOrderId, cancellationToken);

    var remainingExpense = purchaseOrder.Lines.Aggregate(
        Money.Zero,
        (total, line) => total + (line.UnitCostAtOrder * purchaseOrder.GetOutstandingQuantity(line.ProductId)));

    if (remainingExpense.IsZero)
    {
      return;
    }

    var pendingExpense = new FinancialTransaction(
        FinancialTransactionType.PurchaseExpense,
        remainingExpense,
        ReferenceType.PurchaseOrder,
        purchaseOrder.PurchaseOrderId,
        _clock.UtcNow);

    await _transactionRepository.AddAsync(pendingExpense, cancellationToken);
  }

  private static void EnsureReason(string? reason, string message)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ValidationException(message);
    }
  }
}
