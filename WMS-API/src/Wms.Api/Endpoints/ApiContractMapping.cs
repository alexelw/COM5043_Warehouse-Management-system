namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.Common.Models;
  using Wms.Application.Finance;
  using Wms.Application.Inventory;
  using Wms.Application.Orders;
  using Wms.Application.PurchaseOrders;
  using Wms.Application.Reporting;
  using Wms.Application.Suppliers;
  using Wms.Contracts.Common;
  using Wms.Contracts.Finance;
  using Wms.Contracts.Inventory;
  using Wms.Contracts.Orders;
  using Wms.Contracts.PurchaseOrders;
  using Wms.Contracts.Reporting;
  using Wms.Contracts.Suppliers;

  using ApplicationAdjustStockRequest = Wms.Application.Inventory.AdjustStockRequest;
  using ApplicationCancelCustomerOrderRequest = Wms.Application.Orders.CancelCustomerOrderRequest;
  using ApplicationCancelPurchaseOrderRequest = Wms.Application.PurchaseOrders.CancelPurchaseOrderRequest;
  using ApplicationCreateCustomerOrderRequest = Wms.Application.Orders.CreateCustomerOrderRequest;
  using ApplicationCreatePurchaseOrderRequest = Wms.Application.PurchaseOrders.CreatePurchaseOrderRequest;
  using ApplicationCustomerInputModel = Wms.Application.Orders.CustomerInputModel;
  using ApplicationCustomerOrderLineInput = Wms.Application.Orders.CustomerOrderLineInput;
  using ApplicationExportFinancialReportRequest = Wms.Application.Reporting.ExportFinancialReportRequest;
  using ApplicationGoodsReceiptLineInput = Wms.Application.PurchaseOrders.GoodsReceiptLineInput;
  using ApplicationMoneyModel = Wms.Application.Common.Models.MoneyModel;
  using ApplicationPurchaseOrderLineInput = Wms.Application.PurchaseOrders.PurchaseOrderLineInput;
  using ApplicationReceiveDeliveryRequest = Wms.Application.PurchaseOrders.ReceiveDeliveryRequest;
  using ApplicationVoidOrReverseTransactionRequest = Wms.Application.Finance.VoidOrReverseTransactionRequest;
  using ContractAdjustStockRequest = Wms.Contracts.Inventory.AdjustStockRequest;
  using ContractCancelCustomerOrderRequest = Wms.Contracts.Orders.CancelCustomerOrderRequest;
  using ContractCancelPurchaseOrderRequest = Wms.Contracts.PurchaseOrders.CancelPurchaseOrderRequest;
  using ContractCreateCustomerOrderRequest = Wms.Contracts.Orders.CreateCustomerOrderRequest;
  using ContractCreateProductRequest = Wms.Contracts.Inventory.CreateProductRequest;
  using ContractCreatePurchaseOrderRequest = Wms.Contracts.PurchaseOrders.CreatePurchaseOrderRequest;
  using ContractExportFinancialReportRequest = Wms.Contracts.Reporting.ExportFinancialReportRequest;
  using ContractReceiveDeliveryRequest = Wms.Contracts.PurchaseOrders.ReceiveDeliveryRequest;
  using ContractUpdateProductRequest = Wms.Contracts.Inventory.UpdateProductRequest;
  using ContractVoidOrReverseTransactionRequest = Wms.Contracts.Finance.VoidOrReverseTransactionRequest;
  using ReportFormat = Wms.Domain.Enums.ReportFormat;

  internal static class ApiContractMapping
  {
    public static SupplierWriteModel ToWriteModel(this CreateSupplierRequest request)
    {
      return new SupplierWriteModel(request.Name, request.Email, request.Phone, request.Address);
    }

    public static SupplierWriteModel ToWriteModel(this UpdateSupplierRequest request)
    {
      return new SupplierWriteModel(request.Name, request.Email, request.Phone, request.Address);
    }

    public static ProductWriteModel ToWriteModel(this ContractCreateProductRequest request)
    {
      return new ProductWriteModel(
          request.Sku,
          request.Name,
          request.SupplierId,
          request.ReorderThreshold,
          request.UnitCost.ToModel());
    }

    public static ProductWriteModel ToWriteModel(this ContractUpdateProductRequest request)
    {
      return new ProductWriteModel(
          request.Sku,
          request.Name,
          request.SupplierId,
          request.ReorderThreshold,
          request.UnitCost.ToModel());
    }

    public static ApplicationAdjustStockRequest ToApplicationRequest(this ContractAdjustStockRequest request)
    {
      return new ApplicationAdjustStockRequest(request.Quantity, request.Reason);
    }

    public static ApplicationCreatePurchaseOrderRequest ToApplicationRequest(this ContractCreatePurchaseOrderRequest request)
    {
      return new ApplicationCreatePurchaseOrderRequest(
          request.SupplierId,
          request.Lines.Select(line => new ApplicationPurchaseOrderLineInput(
              line.ProductId,
              line.Quantity,
              line.UnitCost.ToModel()))
          .ToArray());
    }

    public static ApplicationReceiveDeliveryRequest ToApplicationRequest(this ContractReceiveDeliveryRequest request)
    {
      return new ApplicationReceiveDeliveryRequest(
          request.Lines.Select(line => new ApplicationGoodsReceiptLineInput(
              line.ProductId,
              line.QuantityReceived))
          .ToArray());
    }

    public static ApplicationCancelPurchaseOrderRequest ToApplicationRequest(this ContractCancelPurchaseOrderRequest request)
    {
      return new ApplicationCancelPurchaseOrderRequest(request.Reason);
    }

    public static ApplicationCreateCustomerOrderRequest ToApplicationRequest(this ContractCreateCustomerOrderRequest request)
    {
      return new ApplicationCreateCustomerOrderRequest(
          new ApplicationCustomerInputModel(
              request.Customer.Name,
              request.Customer.Email,
              request.Customer.Phone),
          request.Lines.Select(line => new ApplicationCustomerOrderLineInput(
              line.ProductId,
              line.Quantity,
              line.UnitPrice.ToModel()))
          .ToArray());
    }

    public static ApplicationCancelCustomerOrderRequest ToApplicationRequest(this ContractCancelCustomerOrderRequest request)
    {
      return new ApplicationCancelCustomerOrderRequest(request.Reason);
    }

    public static ApplicationVoidOrReverseTransactionRequest ToApplicationRequest(
        this ContractVoidOrReverseTransactionRequest request)
    {
      var action = request.Action switch
      {
        "Void" => TransactionAction.Void,
        "Reverse" => TransactionAction.Reverse,
        _ => throw RequestValidationException.ForSingleError("action", "Action must be Void or Reverse."),
      };

      return new ApplicationVoidOrReverseTransactionRequest(action, request.Reason);
    }

    public static ApplicationExportFinancialReportRequest ToApplicationRequest(
        this ContractExportFinancialReportRequest request,
        DateTime? from,
        DateTime? to)
    {
      var format = request.Format switch
      {
        "TXT" => ReportFormat.TXT,
        "JSON" => ReportFormat.JSON,
        _ => throw RequestValidationException.ForSingleError("format", "Format must be TXT or JSON."),
      };

      return new ApplicationExportFinancialReportRequest(format, from, to);
    }

    public static SupplierResponse ToResponse(this SupplierResult result)
    {
      return new SupplierResponse
      {
        SupplierId = result.SupplierId,
        Name = result.Name,
        Email = result.Email,
        Phone = result.Phone,
        Address = result.Address,
      };
    }

    public static ProductResponse ToResponse(this ProductResult result)
    {
      return new ProductResponse
      {
        ProductId = result.ProductId,
        Sku = result.Sku,
        Name = result.Name,
        SupplierId = result.SupplierId,
        ReorderThreshold = result.ReorderThreshold,
        QuantityOnHand = result.QuantityOnHand,
        UnitCost = result.UnitCost.ToDto(),
      };
    }

    public static StockLevelResponse ToResponse(this StockLevelResult result)
    {
      return new StockLevelResponse
      {
        ProductId = result.ProductId,
        Sku = result.Sku,
        Name = result.Name,
        QuantityOnHand = result.QuantityOnHand,
      };
    }

    public static PurchaseOrderResponse ToResponse(this PurchaseOrderResult result)
    {
      return new PurchaseOrderResponse
      {
        PurchaseOrderId = result.PurchaseOrderId,
        SupplierId = result.SupplierId,
        Status = result.Status.ToString(),
        CreatedAt = result.CreatedAt,
        Lines = result.Lines.Select(static line => line.ToResponse()).ToArray(),
      };
    }

    public static PurchaseOrderLineResponse ToResponse(this PurchaseOrderLineResult result)
    {
      return new PurchaseOrderLineResponse
      {
        ProductId = result.ProductId,
        QuantityOrdered = result.QuantityOrdered,
        UnitCost = result.UnitCost.ToDto(),
      };
    }

    public static GoodsReceiptResponse ToResponse(this GoodsReceiptResult result)
    {
      return new GoodsReceiptResponse
      {
        GoodsReceiptId = result.GoodsReceiptId,
        PurchaseOrderId = result.PurchaseOrderId,
        ReceivedAt = result.ReceivedAt,
        Lines = result.Lines.Select(static line => line.ToResponse()).ToArray(),
      };
    }

    public static GoodsReceiptLineResponse ToResponse(this GoodsReceiptLineResult result)
    {
      return new GoodsReceiptLineResponse
      {
        ProductId = result.ProductId,
        QuantityReceived = result.QuantityReceived,
      };
    }

    public static CustomerOrderResponse ToResponse(this CustomerOrderResult result)
    {
      return new CustomerOrderResponse
      {
        CustomerOrderId = result.CustomerOrderId,
        Status = result.Status.ToString(),
        CreatedAt = result.CreatedAt,
        Lines = result.Lines.Select(static line => line.ToResponse()).ToArray(),
        TotalAmount = result.TotalAmount.ToDto(),
      };
    }

    public static CustomerOrderLineResponse ToResponse(this CustomerOrderLineResult result)
    {
      return new CustomerOrderLineResponse
      {
        ProductId = result.ProductId,
        Quantity = result.Quantity,
        UnitPrice = result.UnitPrice.ToDto(),
      };
    }

    public static FinancialTransactionResponse ToResponse(this FinancialTransactionResult result)
    {
      return new FinancialTransactionResponse
      {
        TransactionId = result.TransactionId,
        Type = result.Type.ToString(),
        Status = result.Status.ToString(),
        Amount = result.Amount.ToDto(),
        OccurredAt = result.OccurredAt,
        ReferenceType = result.ReferenceType.ToString(),
        ReferenceId = result.ReferenceId,
        ReversalOfTransactionId = result.ReversalOfTransactionId,
      };
    }

    public static FinancialReportResponse ToResponse(this FinancialReportResult result)
    {
      return new FinancialReportResponse
      {
        From = result.From is null ? null : DateOnly.FromDateTime(result.From.Value),
        To = result.To is null ? null : DateOnly.FromDateTime(result.To.Value),
        TotalSales = result.TotalSales.ToDto(),
        TotalExpenses = result.TotalExpenses.ToDto(),
      };
    }

    public static ReportExportResponse ToResponse(this ReportExportResult result)
    {
      return new ReportExportResponse
      {
        ExportId = result.ReportExportId,
        ReportType = result.ReportType.ToString(),
        Format = result.Format.ToString(),
        GeneratedAt = result.GeneratedAt,
        FilePath = result.FilePath,
      };
    }

    private static ApplicationMoneyModel ToModel(this MoneyDto money)
    {
      return new ApplicationMoneyModel(money.Amount, money.Currency);
    }

    private static MoneyDto ToDto(this MoneyModel money)
    {
      return new MoneyDto
      {
        Amount = money.Amount,
        Currency = money.Currency,
      };
    }
  }
}
