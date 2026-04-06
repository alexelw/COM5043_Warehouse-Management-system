namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.Orders;
  using Wms.Contracts.Orders;
  using Wms.Domain.Enums;

  using ContractCancelCustomerOrderRequest = Wms.Contracts.Orders.CancelCustomerOrderRequest;
  using ContractCreateCustomerOrderRequest = Wms.Contracts.Orders.CreateCustomerOrderRequest;

  internal static class CustomerOrderEndpoints
  {
    private static readonly IReadOnlyDictionary<string, Func<CustomerOrderResult, IComparable?>> CustomerOrderSortSelectors =
        new Dictionary<string, Func<CustomerOrderResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["createdAt"] = customerOrder => customerOrder.CreatedAt,
          ["status"] = customerOrder => customerOrder.Status,
        };

    public static void MapCustomerOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
      var group = endpoints.MapGroup("/api/customer-orders").WithTags("Customer Orders");

      group.MapPost("/", CreateCustomerOrderAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("CreateCustomerOrder", "Create customer order", "Creates and confirms a customer order.")
          .Produces<CustomerOrderResponse>(StatusCodes.Status201Created)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/", GetCustomerOrdersAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetCustomerOrders", "Get customer orders", "Returns customer order history with optional filtering, paging, and sorting.")
          .Produces<CustomerOrderResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/open", GetOpenCustomerOrdersAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("GetOpenCustomerOrders", "Get open customer orders", "Returns customer orders that can still be cancelled by warehouse staff.")
          .Produces<CustomerOrderResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{customerOrderId:guid}", GetCustomerOrderAsync)
          .RequireWmsRole(UserRole.Manager, UserRole.WarehouseStaff)
          .WithWmsDocs("GetCustomerOrder", "Get customer order", "Returns customer order details.")
          .Produces<CustomerOrderResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapPost("/{customerOrderId:guid}/cancel", CancelCustomerOrderAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("CancelCustomerOrder", "Cancel customer order", "Cancels a draft or confirmed customer order and restores stock when required.")
          .Produces<CustomerOrderResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateCustomerOrderAsync(
        ContractCreateCustomerOrderRequest request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var customerOrder = await orderService.CreateCustomerOrderAsync(request.ToApplicationRequest(), cancellationToken);
      return TypedResults.Created($"/api/customer-orders/{customerOrder.CustomerOrderId}", customerOrder.ToResponse());
    }

    private static async Task<IResult> GetCustomerOrdersAsync(
        Guid? customerId,
        string? status,
        string? from,
        string? to,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
      var parsedStatus = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.CustomerOrderStatus>(status, "status");
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var customerOrders = await orderService.GetCustomerOrdersAsync(
          customerId,
          parsedStatus,
          ApiEndpointHelpers.ToStartOfDayUtc(parsedFrom),
          ApiEndpointHelpers.ToEndOfDayUtc(parsedTo),
          cancellationToken);

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          customerOrders,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "createdAt",
          defaultDescending: true,
          CustomerOrderSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static customerOrder => customerOrder.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetOpenCustomerOrdersAsync(
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
      var customerOrders = await orderService.GetCustomerOrdersAsync(cancellationToken: cancellationToken);

      var openCustomerOrders = customerOrders
          .Where(static customerOrder =>
              customerOrder.Status is Wms.Domain.Enums.CustomerOrderStatus.Draft or
              Wms.Domain.Enums.CustomerOrderStatus.Confirmed)
          .ToArray();

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          openCustomerOrders,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "createdAt",
          defaultDescending: true,
          CustomerOrderSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static customerOrder => customerOrder.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetCustomerOrderAsync(
        Guid customerOrderId,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
      var customerOrder = await orderService.GetCustomerOrderAsync(customerOrderId, cancellationToken);
      return TypedResults.Ok(customerOrder.ToResponse());
    }

    private static async Task<IResult> CancelCustomerOrderAsync(
        Guid customerOrderId,
        ContractCancelCustomerOrderRequest request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var customerOrder = await orderService.CancelCustomerOrderAsync(
          customerOrderId,
          request.ToApplicationRequest(),
          cancellationToken);

      return TypedResults.Ok(customerOrder.ToResponse());
    }
  }
}
