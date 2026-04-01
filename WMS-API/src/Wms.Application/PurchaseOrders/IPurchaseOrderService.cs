using Wms.Domain.Enums;

namespace Wms.Application.PurchaseOrders;

public interface IPurchaseOrderService
{
  Task<PurchaseOrderResult> CreatePurchaseOrderAsync(
      CreatePurchaseOrderRequest request,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<PurchaseOrderResult>> GetPurchaseOrdersAsync(
      Guid? supplierId = null,
      PurchaseOrderStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);

  Task<PurchaseOrderResult> GetPurchaseOrderAsync(
      Guid purchaseOrderId,
      CancellationToken cancellationToken = default);

  Task<PurchaseOrderResult> CancelPurchaseOrderAsync(
      Guid purchaseOrderId,
      CancelPurchaseOrderRequest request,
      CancellationToken cancellationToken = default);

  Task<GoodsReceiptResult> ReceiveDeliveryAsync(
      Guid purchaseOrderId,
      ReceiveDeliveryRequest request,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<GoodsReceiptResult>> GetReceiptsAsync(
      Guid purchaseOrderId,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);
}
