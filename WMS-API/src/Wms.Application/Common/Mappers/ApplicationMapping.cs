using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Models;
using Wms.Application.Finance;
using Wms.Application.Inventory;
using Wms.Application.Orders;
using Wms.Application.PurchaseOrders;
using Wms.Application.Reporting;
using Wms.Application.Suppliers;
using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Common.Mappers;

internal static class ApplicationMapping
{
  public static Money ToDomain(this MoneyModel model, string fieldName)
  {
    ArgumentNullException.ThrowIfNull(model);

    if (model.Amount <= 0m)
    {
      throw new ValidationException($"{fieldName} amount must be greater than zero.");
    }

    return new Money(model.Amount, model.Currency);
  }

  public static MoneyModel ToModel(this Money money)
  {
    ArgumentNullException.ThrowIfNull(money);
    return new MoneyModel(money.Amount, money.Currency);
  }

  public static ContactDetails ToRequiredContactDetails(string? email, string? phone, string? address)
  {
    return new ContactDetails(email, phone, address);
  }

  public static ContactDetails? ToOptionalContactDetails(string? email, string? phone, string? address)
  {
    if (string.IsNullOrWhiteSpace(email) &&
        string.IsNullOrWhiteSpace(phone) &&
        string.IsNullOrWhiteSpace(address))
    {
      return null;
    }

    return new ContactDetails(email, phone, address);
  }

  public static DateRange? ToDateRange(DateTime? from, DateTime? to)
  {
    if (from is null && to is null)
    {
      return null;
    }

    return new DateRange(from ?? DateTime.MinValue, to ?? DateTime.MaxValue);
  }

  public static SupplierResult ToResult(this Supplier supplier)
  {
    ArgumentNullException.ThrowIfNull(supplier);

    return new SupplierResult(
        supplier.SupplierId,
        supplier.Name,
        supplier.Contact.Email,
        supplier.Contact.Phone,
        supplier.Contact.Address);
  }

  public static ProductResult ToResult(this Product product)
  {
    ArgumentNullException.ThrowIfNull(product);

    return new ProductResult(
        product.ProductId,
        product.Sku,
        product.Name,
        product.SupplierId,
        product.ReorderLevel,
        product.QuantityOnHand,
        product.UnitCost.ToModel());
  }

  public static StockLevelResult ToStockLevelResult(this Product product)
  {
    ArgumentNullException.ThrowIfNull(product);

    return new StockLevelResult(
        product.ProductId,
        product.Sku,
        product.Name,
        product.QuantityOnHand);
  }

  public static PurchaseOrderResult ToResult(this PurchaseOrder purchaseOrder)
  {
    ArgumentNullException.ThrowIfNull(purchaseOrder);

    return new PurchaseOrderResult(
        purchaseOrder.PurchaseOrderId,
        purchaseOrder.SupplierId,
        purchaseOrder.Status,
        purchaseOrder.CreatedAt,
        purchaseOrder.Lines.Select(line => line.ToResult()).ToArray(),
        purchaseOrder.TotalOrderedAmount.ToModel());
  }

  public static PurchaseOrderLineResult ToResult(this PurchaseOrderLine line)
  {
    ArgumentNullException.ThrowIfNull(line);

    return new PurchaseOrderLineResult(
        line.ProductId,
        line.QuantityOrdered,
        line.UnitCostAtOrder.ToModel());
  }

  public static GoodsReceiptResult ToResult(this GoodsReceipt receipt)
  {
    ArgumentNullException.ThrowIfNull(receipt);

    return new GoodsReceiptResult(
        receipt.GoodsReceiptId,
        receipt.PurchaseOrderId,
        receipt.ReceivedAt,
        receipt.Lines.Select(line => line.ToResult()).ToArray());
  }

  public static GoodsReceiptLineResult ToResult(this GoodsReceiptLine line)
  {
    ArgumentNullException.ThrowIfNull(line);

    return new GoodsReceiptLineResult(line.ProductId, line.QuantityReceived);
  }

  public static CustomerOrderResult ToResult(this CustomerOrder customerOrder)
  {
    ArgumentNullException.ThrowIfNull(customerOrder);

    return new CustomerOrderResult(
        customerOrder.CustomerOrderId,
        customerOrder.CustomerId,
        customerOrder.Status,
        customerOrder.CreatedAt,
        customerOrder.Lines.Select(line => line.ToResult()).ToArray(),
        customerOrder.TotalAmount.ToModel());
  }

  public static CustomerOrderLineResult ToResult(this CustomerOrderLine line)
  {
    ArgumentNullException.ThrowIfNull(line);

    return new CustomerOrderLineResult(
        line.ProductId,
        line.Quantity,
        line.UnitPriceAtSale.ToModel());
  }

  public static FinancialTransactionResult ToResult(this FinancialTransaction transaction)
  {
    ArgumentNullException.ThrowIfNull(transaction);

    return new FinancialTransactionResult(
        transaction.TransactionId,
        transaction.Type,
        transaction.Status,
        transaction.Amount.ToModel(),
        transaction.OccurredAt,
        transaction.ReferenceType,
        transaction.ReferenceId,
        transaction.ReversalOfTransactionId,
        transaction.SignedAmount);
  }

  public static ReportExportResult ToResult(this ReportExport export)
  {
    ArgumentNullException.ThrowIfNull(export);

    var from = export.DateRange?.From == DateTime.MinValue
        ? null
        : export.DateRange?.From;
    var to = export.DateRange?.To == DateTime.MaxValue
        ? null
        : export.DateRange?.To;

    return new ReportExportResult(
        export.ReportExportId,
        export.ReportType,
        export.Format,
        export.GeneratedAt,
        export.FilePath,
        from,
        to);
  }
}
