using Wms.Application.Abstractions;
using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Mappers;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;

namespace Wms.Application.Orders;

public sealed class OrderService : IOrderService
{
  private readonly ICustomerRepository _customerRepository;
  private readonly ICustomerOrderRepository _customerOrderRepository;
  private readonly IProductRepository _productRepository;
  private readonly IStockMovementRepository _stockMovementRepository;
  private readonly ITransactionRepository _transactionRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IClock _clock;

  public OrderService(
      ICustomerRepository customerRepository,
      ICustomerOrderRepository customerOrderRepository,
      IProductRepository productRepository,
      IStockMovementRepository stockMovementRepository,
      ITransactionRepository transactionRepository,
      IUnitOfWork unitOfWork,
      IClock clock)
  {
    _customerRepository = customerRepository;
    _customerOrderRepository = customerOrderRepository;
    _productRepository = productRepository;
    _stockMovementRepository = stockMovementRepository;
    _transactionRepository = transactionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
  }

  public async Task<CustomerOrderResult> CreateCustomerOrderAsync(
      CreateCustomerOrderRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(request.Customer);
    ArgumentNullException.ThrowIfNull(request.Lines);

    var occurredAt = _clock.UtcNow;
    var customer = new Customer(
        request.Customer.Name,
        ApplicationMapping.ToOptionalContactDetails(request.Customer.Email, request.Customer.Phone, null));

    var products = await LoadProductsAsync(
        request.Lines.Select(line => line.ProductId),
        cancellationToken);

    var lines = request.Lines
        .Select(line => new CustomerOrderLine(
            line.ProductId,
            line.Quantity,
            line.UnitPrice.ToDomain(nameof(line.UnitPrice))))
        .ToArray();

    var customerOrder = new CustomerOrder(customer.CustomerId, lines, occurredAt);
    customerOrder.Confirm(products.Values);

    var stockMovements = new List<StockMovement>();
    foreach (var lineGroup in customerOrder.Lines.GroupBy(line => line.ProductId))
    {
      var quantity = lineGroup.Sum(line => line.Quantity);
      var product = products[lineGroup.Key];
      product.DecreaseStock(quantity);

      stockMovements.Add(StockMovement.CreateIssue(
          lineGroup.Key,
          quantity,
          ReferenceType.CustomerOrder,
          customerOrder.CustomerOrderId,
          occurredAt));
    }

    var saleTransaction = new FinancialTransaction(
        FinancialTransactionType.Sale,
        customerOrder.TotalAmount,
        ReferenceType.CustomerOrder,
        customerOrder.CustomerOrderId,
        occurredAt);
    saleTransaction.MarkPosted();

    await _customerRepository.AddAsync(customer, cancellationToken);
    await _customerOrderRepository.AddAsync(customerOrder, cancellationToken);

    foreach (var product in products.Values)
    {
      await _productRepository.UpdateAsync(product, cancellationToken);
    }

    await _stockMovementRepository.AddRangeAsync(stockMovements, cancellationToken);
    await _transactionRepository.AddAsync(saleTransaction, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return customerOrder.ToResult();
  }

  public async Task<IReadOnlyList<CustomerOrderResult>> GetCustomerOrdersAsync(
      Guid? customerId = null,
      CustomerOrderStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    var customerOrders = await _customerOrderRepository.ListAsync(
        customerId,
        status,
        ApplicationMapping.ToDateRange(from, to),
        cancellationToken);

    return customerOrders.Select(customerOrder => customerOrder.ToResult()).ToArray();
  }

  public async Task<CustomerOrderResult> GetCustomerOrderAsync(
      Guid customerOrderId,
      CancellationToken cancellationToken = default)
  {
    var customerOrder = await GetCustomerOrderEntityAsync(customerOrderId, cancellationToken);
    return customerOrder.ToResult();
  }

  public async Task<CustomerOrderResult> CancelCustomerOrderAsync(
      Guid customerOrderId,
      CancelCustomerOrderRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    EnsureReason(request.Reason, "Cancellation reason is required.");

    var customerOrder = await GetCustomerOrderEntityAsync(customerOrderId, cancellationToken);
    var wasConfirmed = customerOrder.Status == CustomerOrderStatus.Confirmed;
    var occurredAt = _clock.UtcNow;

    customerOrder.Cancel();
    await _customerOrderRepository.UpdateAsync(customerOrder, cancellationToken);

    if (wasConfirmed)
    {
      var products = await LoadProductsAsync(
          customerOrder.Lines.Select(line => line.ProductId),
          cancellationToken);

      var stockMovements = new List<StockMovement>();
      foreach (var lineGroup in customerOrder.Lines.GroupBy(line => line.ProductId))
      {
        var quantity = lineGroup.Sum(line => line.Quantity);
        var product = products[lineGroup.Key];
        product.IncreaseStock(quantity);

        stockMovements.Add(StockMovement.CreateReceipt(
            lineGroup.Key,
            quantity,
            ReferenceType.CustomerOrder,
            customerOrder.CustomerOrderId,
            occurredAt));
      }

      foreach (var product in products.Values)
      {
        await _productRepository.UpdateAsync(product, cancellationToken);
      }

      await _stockMovementRepository.AddRangeAsync(stockMovements, cancellationToken);
    }

    await UpdateCustomerOrderTransactionsAsync(customerOrder.CustomerOrderId, wasConfirmed, occurredAt, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return customerOrder.ToResult();
  }

  private async Task<CustomerOrder> GetCustomerOrderEntityAsync(
      Guid customerOrderId,
      CancellationToken cancellationToken)
  {
    var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId, cancellationToken);
    if (customerOrder is null)
    {
      throw new NotFoundException(nameof(CustomerOrder), customerOrderId);
    }

    return customerOrder;
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

  private async Task UpdateCustomerOrderTransactionsAsync(
      Guid customerOrderId,
      bool wasConfirmed,
      DateTime occurredAt,
      CancellationToken cancellationToken)
  {
    var transactions = await _transactionRepository.ListByReferenceAsync(
        ReferenceType.CustomerOrder,
        customerOrderId,
        cancellationToken);

    foreach (var transaction in transactions.Where(static transaction => transaction.Status == FinancialTransactionStatus.Pending))
    {
      transaction.MarkVoided();
      await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }

    if (!wasConfirmed)
    {
      return;
    }

    foreach (var transaction in transactions.Where(static transaction =>
                 transaction.Status == FinancialTransactionStatus.Posted &&
                 !transaction.IsReversal))
    {
      var reversal = transaction.CreateReversal(occurredAt);
      await _transactionRepository.UpdateAsync(transaction, cancellationToken);
      await _transactionRepository.AddAsync(reversal, cancellationToken);
    }
  }

  private static void EnsureReason(string? reason, string message)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ValidationException(message);
    }
  }
}
