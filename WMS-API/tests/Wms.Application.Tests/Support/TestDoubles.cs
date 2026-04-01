using Wms.Application.Abstractions;
using Wms.Application.Reporting;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Tests.Support;

internal sealed class FakeClock : IClock
{
  public DateTime UtcNow { get; set; } = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
}

internal sealed class TrackingUnitOfWork : IUnitOfWork
{
  public int SaveChangesCalls { get; private set; }

  public Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    SaveChangesCalls++;
    return Task.CompletedTask;
  }
}

internal sealed class FakeReportExporter : IReportExporter
{
  public string FilePath { get; set; } = "exports/report.txt";

  public Task<string> ExportFinancialReportAsync(
      FinancialReportResult report,
      ReportFormat format,
      CancellationToken cancellationToken = default)
  {
    return Task.FromResult(FilePath);
  }
}

internal sealed class InMemorySupplierRepository : ISupplierRepository
{
  private readonly List<Supplier> _suppliers = new();

  public Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_suppliers.SingleOrDefault(supplier => supplier.SupplierId == supplierId));
  }

  public Task<IReadOnlyList<Supplier>> ListAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyList<Supplier>>(_suppliers.ToArray());
  }

  public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
  {
    _suppliers.Add(supplier);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }

  public Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    _suppliers.RemoveAll(supplier => supplier.SupplierId == supplierId);
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryProductRepository : IProductRepository
{
  private readonly List<Product> _products = new();

  public HashSet<Guid> InUseProductIds { get; } = new();

  public IReadOnlyList<Product> Items => _products;

  public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_products.SingleOrDefault(product => product.ProductId == productId));
  }

  public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_products.SingleOrDefault(product => product.Sku == sku));
  }

  public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_products.Any(product => product.Sku == sku));
  }

  public Task<IReadOnlyList<Product>> ListAsync(
      Guid? supplierId = null,
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<Product> query = _products;

    if (supplierId.HasValue)
    {
      query = query.Where(product => product.SupplierId == supplierId.Value);
    }

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      query = query.Where(product =>
          product.Sku.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
          product.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    return Task.FromResult<IReadOnlyList<Product>>(query.ToArray());
  }

  public Task<IReadOnlyList<Product>> ListLowStockAsync(
      string? searchTerm = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<Product> query = _products.Where(product => product.IsLowStock);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      query = query.Where(product =>
          product.Sku.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
          product.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    return Task.FromResult<IReadOnlyList<Product>>(query.ToArray());
  }

  public Task AddAsync(Product product, CancellationToken cancellationToken = default)
  {
    _products.Add(product);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }

  public Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    _products.RemoveAll(product => product.ProductId == productId);
    return Task.CompletedTask;
  }

  public Task<bool> IsInUseAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(this.InUseProductIds.Contains(productId));
  }
}

internal sealed class InMemoryPurchaseOrderRepository : IPurchaseOrderRepository
{
  private readonly List<PurchaseOrder> _purchaseOrders = new();

  public Task<PurchaseOrder?> GetByIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_purchaseOrders.SingleOrDefault(order => order.PurchaseOrderId == purchaseOrderId));
  }

  public Task<IReadOnlyList<PurchaseOrder>> ListAsync(
      Guid? supplierId = null,
      PurchaseOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<PurchaseOrder> query = _purchaseOrders;

    if (supplierId.HasValue)
    {
      query = query.Where(order => order.SupplierId == supplierId.Value);
    }

    if (status.HasValue)
    {
      query = query.Where(order => order.Status == status.Value);
    }

    if (createdWithin is not null)
    {
      query = query.Where(order => createdWithin.Contains(order.CreatedAt));
    }

    return Task.FromResult<IReadOnlyList<PurchaseOrder>>(query.ToArray());
  }

  public Task<IReadOnlyList<PurchaseOrder>> ListBySupplierAsync(
      Guid supplierId,
      PurchaseOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default)
  {
    return ListAsync(supplierId, status, createdWithin, cancellationToken);
  }

  public Task<bool> HasOpenOrdersForSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_purchaseOrders.Any(order =>
        order.SupplierId == supplierId &&
        order.Status is PurchaseOrderStatus.Pending or PurchaseOrderStatus.PartiallyReceived));
  }

  public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
  {
    _purchaseOrders.Add(purchaseOrder);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryGoodsReceiptRepository : IGoodsReceiptRepository
{
  private readonly List<GoodsReceipt> _goodsReceipts = new();

  public Task<GoodsReceipt?> GetByIdAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_goodsReceipts.SingleOrDefault(receipt => receipt.GoodsReceiptId == goodsReceiptId));
  }

  public Task<IReadOnlyList<GoodsReceipt>> ListByPurchaseOrderAsync(
      Guid purchaseOrderId,
      DateRange? receivedWithin = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<GoodsReceipt> query = _goodsReceipts.Where(receipt => receipt.PurchaseOrderId == purchaseOrderId);

    if (receivedWithin is not null)
    {
      query = query.Where(receipt => receivedWithin.Contains(receipt.ReceivedAt));
    }

    return Task.FromResult<IReadOnlyList<GoodsReceipt>>(query.ToArray());
  }

  public Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
  {
    _goodsReceipts.Add(goodsReceipt);
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryCustomerRepository : ICustomerRepository
{
  private readonly List<Customer> _customers = new();

  public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_customers.SingleOrDefault(customer => customer.CustomerId == customerId));
  }

  public Task<IReadOnlyList<Customer>> ListAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
  {
    IEnumerable<Customer> query = _customers;

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      query = query.Where(customer => customer.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    return Task.FromResult<IReadOnlyList<Customer>>(query.ToArray());
  }

  public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
  {
    _customers.Add(customer);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryCustomerOrderRepository : ICustomerOrderRepository
{
  private readonly List<CustomerOrder> _customerOrders = new();

  public Task<CustomerOrder?> GetByIdAsync(Guid customerOrderId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_customerOrders.SingleOrDefault(order => order.CustomerOrderId == customerOrderId));
  }

  public Task<IReadOnlyList<CustomerOrder>> ListAsync(
      Guid? customerId = null,
      CustomerOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<CustomerOrder> query = _customerOrders;

    if (customerId.HasValue)
    {
      query = query.Where(order => order.CustomerId == customerId.Value);
    }

    if (status.HasValue)
    {
      query = query.Where(order => order.Status == status.Value);
    }

    if (createdWithin is not null)
    {
      query = query.Where(order => createdWithin.Contains(order.CreatedAt));
    }

    return Task.FromResult<IReadOnlyList<CustomerOrder>>(query.ToArray());
  }

  public Task AddAsync(CustomerOrder customerOrder, CancellationToken cancellationToken = default)
  {
    _customerOrders.Add(customerOrder);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(CustomerOrder customerOrder, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryStockMovementRepository : IStockMovementRepository
{
  private readonly List<StockMovement> _stockMovements = new();

  public IReadOnlyList<StockMovement> Items => _stockMovements;

  public Task<IReadOnlyList<StockMovement>> ListAsync(
      Guid? productId = null,
      StockMovementType? type = null,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<StockMovement> query = _stockMovements;

    if (productId.HasValue)
    {
      query = query.Where(movement => movement.ProductId == productId.Value);
    }

    if (type.HasValue)
    {
      query = query.Where(movement => movement.Type == type.Value);
    }

    if (occurredWithin is not null)
    {
      query = query.Where(movement => occurredWithin.Contains(movement.OccurredAt));
    }

    return Task.FromResult<IReadOnlyList<StockMovement>>(query.ToArray());
  }

  public Task<IReadOnlyList<StockMovement>> ListByProductAsync(
      Guid productId,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default)
  {
    return ListAsync(productId, null, occurredWithin, cancellationToken);
  }

  public Task AddAsync(StockMovement stockMovement, CancellationToken cancellationToken = default)
  {
    _stockMovements.Add(stockMovement);
    return Task.CompletedTask;
  }

  public Task AddRangeAsync(IEnumerable<StockMovement> stockMovements, CancellationToken cancellationToken = default)
  {
    _stockMovements.AddRange(stockMovements);
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryTransactionRepository : ITransactionRepository
{
  private readonly List<FinancialTransaction> _transactions = new();

  public IReadOnlyList<FinancialTransaction> Items => _transactions;

  public Task<FinancialTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_transactions.SingleOrDefault(transaction => transaction.TransactionId == transactionId));
  }

  public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
      FinancialTransactionType? type = null,
      FinancialTransactionStatus? status = null,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<FinancialTransaction> query = _transactions;

    if (type.HasValue)
    {
      query = query.Where(transaction => transaction.Type == type.Value);
    }

    if (status.HasValue)
    {
      query = query.Where(transaction => transaction.Status == status.Value);
    }

    if (occurredWithin is not null)
    {
      query = query.Where(transaction => occurredWithin.Contains(transaction.OccurredAt));
    }

    return Task.FromResult<IReadOnlyList<FinancialTransaction>>(query.ToArray());
  }

  public Task<IReadOnlyList<FinancialTransaction>> ListByReferenceAsync(
      ReferenceType referenceType,
      Guid referenceId,
      CancellationToken cancellationToken = default)
  {
    var transactions = _transactions
        .Where(transaction => transaction.ReferenceType == referenceType && transaction.ReferenceId == referenceId)
        .ToArray();

    return Task.FromResult<IReadOnlyList<FinancialTransaction>>(transactions);
  }

  public Task AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
  {
    _transactions.Add(transaction);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryReportExportRepository : IReportExportRepository
{
  private readonly List<ReportExport> _exports = new();

  public IReadOnlyList<ReportExport> Items => _exports;

  public Task<ReportExport?> GetByIdAsync(Guid reportExportId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_exports.SingleOrDefault(export => export.ReportExportId == reportExportId));
  }

  public Task<IReadOnlyList<ReportExport>> ListAsync(
      ReportType? reportType = null,
      ReportFormat? format = null,
      DateRange? generatedWithin = null,
      CancellationToken cancellationToken = default)
  {
    IEnumerable<ReportExport> query = _exports;

    if (reportType.HasValue)
    {
      query = query.Where(export => export.ReportType == reportType.Value);
    }

    if (format.HasValue)
    {
      query = query.Where(export => export.Format == format.Value);
    }

    if (generatedWithin is not null)
    {
      query = query.Where(export => generatedWithin.Contains(export.GeneratedAt));
    }

    return Task.FromResult<IReadOnlyList<ReportExport>>(query.ToArray());
  }

  public Task AddAsync(ReportExport reportExport, CancellationToken cancellationToken = default)
  {
    _exports.Add(reportExport);
    return Task.CompletedTask;
  }
}
