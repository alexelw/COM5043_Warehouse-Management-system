namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.Inventory;
  using Wms.Contracts.Inventory;
  using Wms.Domain.Enums;

  using ContractAdjustStockRequest = Wms.Contracts.Inventory.AdjustStockRequest;
  using ContractCreateProductRequest = Wms.Contracts.Inventory.CreateProductRequest;
  using ContractUpdateProductRequest = Wms.Contracts.Inventory.UpdateProductRequest;

  internal static class ProductEndpoints
  {
    private static readonly IReadOnlyDictionary<string, Func<ProductResult, IComparable?>> ProductSortSelectors =
        new Dictionary<string, Func<ProductResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["name"] = product => product.Name,
          ["sku"] = product => product.Sku,
          ["quantityOnHand"] = product => product.QuantityOnHand,
          ["reorderThreshold"] = product => product.ReorderThreshold,
        };

    private static readonly IReadOnlyDictionary<string, Func<StockLevelResult, IComparable?>> StockLevelSortSelectors =
        new Dictionary<string, Func<StockLevelResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["quantityOnHand"] = product => product.QuantityOnHand,
          ["sku"] = product => product.Sku,
          ["name"] = product => product.Name,
        };

    private static readonly IReadOnlyDictionary<string, Func<ProductResult, IComparable?>> LowStockSortSelectors =
        new Dictionary<string, Func<ProductResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["quantityOnHand"] = product => product.QuantityOnHand,
          ["reorderThreshold"] = product => product.ReorderThreshold,
          ["name"] = product => product.Name,
        };

    public static void MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
      var group = endpoints.MapGroup("/api/products").WithTags("Products");

      group.MapGet("/stock", GetStockLevelsAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("GetStockLevels", "Get stock levels", "Returns current stock levels for products.")
          .Produces<StockLevelResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/low-stock", GetLowStockProductsAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetLowStockProducts", "Get low stock products", "Returns products that are at or below their reorder threshold.")
          .Produces<ProductResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapPost("/", CreateProductAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("CreateProduct", "Create product", "Creates a new product.")
          .Produces<ProductResponse>(StatusCodes.Status201Created)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/", GetProductsAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetProducts", "Get products", "Returns the product catalogue with optional filtering, paging, and sorting.")
          .Produces<ProductResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{productId:guid}", GetProductAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("GetProduct", "Get product", "Returns product details.")
          .Produces<ProductResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapPut("/{productId:guid}", UpdateProductAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("UpdateProduct", "Update product", "Updates product details.")
          .Produces<ProductResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapDelete("/{productId:guid}", DeleteProductAsync)
          .RequireWmsRole(UserRole.Manager)
          .WithWmsDocs("DeleteProduct", "Delete product", "Deletes a product.")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);

      group.MapPost("/{productId:guid}/adjust-stock", AdjustStockAsync)
          .RequireWmsRole(UserRole.WarehouseStaff)
          .WithWmsDocs("AdjustStock", "Adjust stock", "Applies a manual stock adjustment to a product.")
          .Produces<StockLevelResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateProductAsync(
        ContractCreateProductRequest request,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var product = await inventoryService.CreateProductAsync(request.ToWriteModel(), cancellationToken);
      return TypedResults.Created($"/api/products/{product.ProductId}", product.ToResponse());
    }

    private static async Task<IResult> GetProductsAsync(
        Guid? supplierId,
        string? q,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      var products = await inventoryService.GetProductsAsync(supplierId, q, cancellationToken);
      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          products,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "name",
          defaultDescending: false,
          ProductSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static product => product.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetProductAsync(
        Guid productId,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      var product = await inventoryService.GetProductAsync(productId, cancellationToken);
      return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid productId,
        ContractUpdateProductRequest request,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var product = await inventoryService.UpdateProductAsync(productId, request.ToWriteModel(), cancellationToken);
      return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<IResult> DeleteProductAsync(
        Guid productId,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      await inventoryService.DeleteProductAsync(productId, cancellationToken);
      return TypedResults.NoContent();
    }

    private static async Task<IResult> GetStockLevelsAsync(
        string? q,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      var stockLevels = await inventoryService.GetStockLevelsAsync(q, cancellationToken);
      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          stockLevels,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "quantityOnHand",
          defaultDescending: true,
          StockLevelSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static stockLevel => stockLevel.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetLowStockProductsAsync(
        string? q,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      var products = await inventoryService.GetLowStockProductsAsync(q, cancellationToken);
      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          products,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "quantityOnHand",
          defaultDescending: false,
          LowStockSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static product => product.ToResponse()).ToArray());
    }

    private static async Task<IResult> AdjustStockAsync(
        Guid productId,
        ContractAdjustStockRequest request,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var stockLevel = await inventoryService.AdjustStockAsync(productId, request.ToApplicationRequest(), cancellationToken);
      return TypedResults.Ok(stockLevel.ToResponse());
    }
  }
}
