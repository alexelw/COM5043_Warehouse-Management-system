namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.PurchaseOrders;
  using Wms.Contracts.PurchaseOrders;
  using Wms.Domain.Enums;

  using ContractCancelPurchaseOrderRequest = Wms.Contracts.PurchaseOrders.CancelPurchaseOrderRequest;
  using ContractCreatePurchaseOrderRequest = Wms.Contracts.PurchaseOrders.CreatePurchaseOrderRequest;
  using ContractReceiveDeliveryRequest = Wms.Contracts.PurchaseOrders.ReceiveDeliveryRequest;

  internal static class PurchaseOrderEndpoints
  {
    private static readonly IReadOnlyDictionary<string, Func<PurchaseOrderResult, IComparable?>> PurchaseOrderSortSelectors =
        new Dictionary<string, Func<PurchaseOrderResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["createdAt"] = purchaseOrder => purchaseOrder.CreatedAt,
          ["status"] = purchaseOrder => purchaseOrder.Status,
        };

    private static readonly IReadOnlyDictionary<string, Func<GoodsReceiptResult, IComparable?>> ReceiptSortSelectors =
        new Dictionary<string, Func<GoodsReceiptResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["receivedAt"] = receipt => receipt.ReceivedAt,
        };

    public static void MapPurchaseOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
      var group = endpoints.MapGroup("/api/purchase-orders").WithTags("Purchase Orders");

      group.MapPost("/", CreatePurchaseOrderAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("CreatePurchaseOrder", "Create purchase order", "Creates a new purchase order.")
          .Produces<PurchaseOrderResponse>(StatusCodes.Status201Created)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/", GetPurchaseOrdersAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetPurchaseOrders", "Get purchase orders", "Returns purchase orders with optional filtering, paging, and sorting.")
          .Produces<PurchaseOrderResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/open", GetOpenPurchaseOrdersAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("GetOpenPurchaseOrders", "Get open purchase orders", "Returns purchase orders that still have quantities outstanding for receiving.")
          .Produces<PurchaseOrderResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{purchaseOrderId:guid}", GetPurchaseOrderAsync)
          .RequireWmsRole(UserRole.Manager, UserRole.WarehouseStaff)
          .WithWmsDocs("GetPurchaseOrder", "Get purchase order", "Returns purchase order details.")
          .Produces<PurchaseOrderResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapPost("/{purchaseOrderId:guid}/cancel", CancelPurchaseOrderAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("CancelPurchaseOrder", "Cancel purchase order", "Cancels a pending or partially received purchase order.")
          .Produces<PurchaseOrderResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapPost("/{purchaseOrderId:guid}/receipts", ReceiveDeliveryAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("ReceiveDelivery", "Receive delivery", "Records a full or partial delivery against a purchase order.")
          .Produces<GoodsReceiptResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{purchaseOrderId:guid}/receipts", GetReceiptsAsync)
          .RequireWmsRole(UserRole.Manager, UserRole.WarehouseStaff)
          .WithWmsDocs("GetPurchaseOrderReceipts", "Get purchase order receipts", "Returns delivery history for a purchase order.")
          .Produces<GoodsReceiptResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreatePurchaseOrderAsync(
        ContractCreatePurchaseOrderRequest request,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var purchaseOrder = await purchaseOrderService.CreatePurchaseOrderAsync(request.ToApplicationRequest(), cancellationToken);
      return TypedResults.Created($"/api/purchase-orders/{purchaseOrder.PurchaseOrderId}", purchaseOrder.ToResponse());
    }

    private static async Task<IResult> GetPurchaseOrdersAsync(
        Guid? supplierId,
        string? status,
        string? from,
        string? to,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      var parsedStatus = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.PurchaseOrderStatus>(status, "status");
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var purchaseOrders = await purchaseOrderService.GetPurchaseOrdersAsync(
          supplierId,
          parsedStatus,
          ApiEndpointHelpers.ToStartOfDayUtc(parsedFrom),
          ApiEndpointHelpers.ToEndOfDayUtc(parsedTo),
          cancellationToken);

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          purchaseOrders,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "createdAt",
          defaultDescending: true,
          PurchaseOrderSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static purchaseOrder => purchaseOrder.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetOpenPurchaseOrdersAsync(
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      var purchaseOrders = await purchaseOrderService.GetPurchaseOrdersAsync(cancellationToken: cancellationToken);

      var openPurchaseOrders = purchaseOrders
          .Where(static purchaseOrder =>
              purchaseOrder.Status is Wms.Domain.Enums.PurchaseOrderStatus.Pending or
              Wms.Domain.Enums.PurchaseOrderStatus.PartiallyReceived)
          .ToArray();

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          openPurchaseOrders,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "createdAt",
          defaultDescending: true,
          PurchaseOrderSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static purchaseOrder => purchaseOrder.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetPurchaseOrderAsync(
        Guid purchaseOrderId,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      var purchaseOrder = await purchaseOrderService.GetPurchaseOrderAsync(purchaseOrderId, cancellationToken);
      return TypedResults.Ok(purchaseOrder.ToResponse());
    }

    private static async Task<IResult> CancelPurchaseOrderAsync(
        Guid purchaseOrderId,
        ContractCancelPurchaseOrderRequest request,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var purchaseOrder = await purchaseOrderService.CancelPurchaseOrderAsync(
          purchaseOrderId,
          request.ToApplicationRequest(),
          cancellationToken);

      return TypedResults.Ok(purchaseOrder.ToResponse());
    }

    private static async Task<IResult> ReceiveDeliveryAsync(
        Guid purchaseOrderId,
        ContractReceiveDeliveryRequest request,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var goodsReceipt = await purchaseOrderService.ReceiveDeliveryAsync(
          purchaseOrderId,
          request.ToApplicationRequest(),
          cancellationToken);

      return TypedResults.Ok(goodsReceipt.ToResponse());
    }

    private static async Task<IResult> GetReceiptsAsync(
        Guid purchaseOrderId,
        string? from,
        string? to,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IPurchaseOrderService purchaseOrderService,
        CancellationToken cancellationToken)
    {
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var receipts = await purchaseOrderService.GetReceiptsAsync(
          purchaseOrderId,
          ApiEndpointHelpers.ToStartOfDayUtc(parsedFrom),
          ApiEndpointHelpers.ToEndOfDayUtc(parsedTo),
          cancellationToken);

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          receipts,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "receivedAt",
          defaultDescending: true,
          ReceiptSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static receipt => receipt.ToResponse()).ToArray());
    }
  }
}
