namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.PurchaseOrders;
  using Wms.Application.Suppliers;
  using Wms.Contracts.PurchaseOrders;
  using Wms.Contracts.Suppliers;
  using Wms.Domain.Enums;

  internal static class SupplierEndpoints
  {
    private static readonly IReadOnlyDictionary<string, Func<SupplierResult, IComparable?>> SupplierSortSelectors =
        new Dictionary<string, Func<SupplierResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["name"] = supplier => supplier.Name,
        };

    private static readonly IReadOnlyDictionary<string, Func<PurchaseOrderResult, IComparable?>> PurchaseOrderSortSelectors =
        new Dictionary<string, Func<PurchaseOrderResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["createdAt"] = purchaseOrder => purchaseOrder.CreatedAt,
          ["status"] = purchaseOrder => purchaseOrder.Status,
        };

    public static void MapSupplierEndpoints(this IEndpointRouteBuilder endpoints)
    {
      var group = endpoints.MapGroup("/api/suppliers").WithTags("Suppliers");

      group.MapPost("/", CreateSupplierAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("CreateSupplier", "Create supplier", "Creates a new supplier record.")
          .Produces<SupplierResponse>(StatusCodes.Status201Created)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/", GetSuppliersAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetSuppliers", "Get suppliers", "Returns suppliers with optional paging, sorting, and search.")
          .Produces<SupplierResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{supplierId:guid}", GetSupplierAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetSupplier", "Get supplier", "Returns a specific supplier.")
          .Produces<SupplierResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapPut("/{supplierId:guid}", UpdateSupplierAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("UpdateSupplier", "Update supplier", "Updates supplier details.")
          .Produces<SupplierResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapDelete("/{supplierId:guid}", DeleteSupplierAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("DeleteSupplier", "Delete supplier", "Deletes a supplier when it is not referenced by open purchase orders.")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{supplierId:guid}/purchase-orders", GetSupplierPurchaseOrdersAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs(
              "GetSupplierPurchaseOrders",
              "Get supplier purchase orders",
              "Returns purchase order history for a supplier.")
          .Produces<PurchaseOrderResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateSupplierAsync(
        CreateSupplierRequest request,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var supplier = await supplierService.CreateSupplierAsync(request.ToWriteModel(), cancellationToken);
      return TypedResults.Created($"/api/suppliers/{supplier.SupplierId}", supplier.ToResponse());
    }

    private static async Task<IResult> GetSuppliersAsync(
        string? q,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
      var suppliers = await supplierService.GetSuppliersAsync(q, cancellationToken);
      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          suppliers,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "name",
          defaultDescending: false,
          SupplierSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static supplier => supplier.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetSupplierAsync(
        Guid supplierId,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
      var supplier = await supplierService.GetSupplierAsync(supplierId, cancellationToken);
      return TypedResults.Ok(supplier.ToResponse());
    }

    private static async Task<IResult> UpdateSupplierAsync(
        Guid supplierId,
        UpdateSupplierRequest request,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var supplier = await supplierService.UpdateSupplierAsync(supplierId, request.ToWriteModel(), cancellationToken);
      return TypedResults.Ok(supplier.ToResponse());
    }

    private static async Task<IResult> DeleteSupplierAsync(
        Guid supplierId,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
      await supplierService.DeleteSupplierAsync(supplierId, cancellationToken);
      return TypedResults.NoContent();
    }

    private static async Task<IResult> GetSupplierPurchaseOrdersAsync(
        Guid supplierId,
        string? status,
        string? from,
        string? to,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
      var parsedStatus = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.PurchaseOrderStatus>(status, "status");
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var purchaseOrders = await supplierService.GetSupplierPurchaseOrdersAsync(
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
  }
}
