using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Wms.Api.Endpoints;
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
using Wms.Contracts.System;
using Wms.Domain.Enums;

using ContractAdjustStockRequest = Wms.Contracts.Inventory.AdjustStockRequest;
using ContractExportFinancialReportRequest = Wms.Contracts.Reporting.ExportFinancialReportRequest;
using ContractVoidOrReverseTransactionRequest = Wms.Contracts.Finance.VoidOrReverseTransactionRequest;

namespace Wms.Application.Tests;

public sealed class ApiRoleAndSwaggerTests
{
  [Fact]
  public async Task HealthEndpoint_IsPublic_ReturnsHealthyStatus()
  {
    await using var host = await TestApiHost.StartAsync();

    using var response = await host.Client.GetAsync("/api/health");
    var health = await ReadRequiredJsonAsync<HealthResponse>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(health);
    Assert.Equal("WMS API", health!.Name);
    Assert.Equal("Healthy", health.Status);
  }

  [Fact]
  public async Task ProtectedEndpoint_WhenRoleHeaderMissing_ReturnsBadRequest()
  {
    await using var host = await TestApiHost.StartAsync();

    using var response = await host.Client.GetAsync("/api/products/stock");
    var error = await ReadRequiredJsonAsync<ErrorResponse>(response);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("validation_failed", error!.Code);
    Assert.Equal(
        "'X-Wms-Role' header is required. Allowed values: WarehouseStaff, Manager, Administrator.",
        error.Errors["role"].Single());
  }

  [Fact]
  public async Task ProtectedEndpoint_WhenRoleNotAllowed_ReturnsForbidden()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/stock");
    request.Headers.Add("X-Wms-Role", UserRole.Manager.ToString());

    using var response = await host.Client.SendAsync(request);
    var error = await ReadRequiredJsonAsync<ErrorResponse>(response);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("forbidden", error!.Code);
    Assert.Contains("Manager", error.Message, StringComparison.Ordinal);
    Assert.Empty(error.Errors);
  }

  [Fact]
  public async Task ProtectedEndpoint_WhenRoleAllowed_ReturnsStockLevels()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/stock");
    request.Headers.Add("X-Wms-Role", UserRole.WarehouseStaff.ToString());

    using var response = await host.Client.SendAsync(request);
    var stockLevels = await ReadRequiredJsonAsync<IReadOnlyList<StockLevelResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var stockLevel = Assert.Single(stockLevels!);
    Assert.Equal("SKU-001", stockLevel.Sku);
  }

  [Fact]
  public async Task SwaggerJson_DocumentsRoleHeadersAndContractConstraints()
  {
    await using var host = await TestApiHost.StartAsync();

    var swaggerJson = await host.Client.GetStringAsync("/swagger/v1/swagger.json");
    var document = JsonNode.Parse(swaggerJson)!.AsObject();

    var productStockParameters = document["paths"]!["/api/products/stock"]!["get"]!["parameters"]!.AsArray();
    var roleHeader = productStockParameters.Single(parameter =>
        string.Equals(parameter!["name"]!.GetValue<string>(), "X-Wms-Role", StringComparison.Ordinal));

    var createProductRequired = document["components"]!["schemas"]!["CreateProductRequest"]!["required"]!.AsArray();
    var transactionActionEnum = document["components"]!["schemas"]!["VoidOrReverseTransactionRequest"]!["properties"]!["action"]!["enum"]!.AsArray();
    var exportFormatEnum = document["components"]!["schemas"]!["ExportFinancialReportRequest"]!["properties"]!["format"]!["enum"]!.AsArray();
    var purchaseOrderStatusParameter = document["paths"]!["/api/purchase-orders"]!["get"]!["parameters"]!
        .AsArray()
        .Single(parameter => string.Equals(parameter!["name"]!.GetValue<string>(), "status", StringComparison.Ordinal));

    Assert.True(roleHeader!["required"]!.GetValue<bool>());
    Assert.Equal("header", roleHeader["in"]!.GetValue<string>());
    Assert.Equal(
        [UserRole.WarehouseStaff.ToString()],
        roleHeader["schema"]!["enum"]!.AsArray().Select(static value => value!.GetValue<string>()).ToArray());
    Assert.Contains(
        "supplierId",
        createProductRequired.Select(static item => item!.GetValue<string>()));
    Assert.Equal(["Void", "Reverse"], transactionActionEnum.Select(static item => item!.GetValue<string>()).ToArray());
    Assert.Equal(["TXT", "JSON"], exportFormatEnum.Select(static item => item!.GetValue<string>()).ToArray());
    Assert.Contains(nameof(PurchaseOrderStatus.Pending), purchaseOrderStatusParameter!["description"]!.GetValue<string>(), StringComparison.Ordinal);
  }

  [Fact]
  public async Task SupplierEndpoint_WhenRoleAllowed_ReturnsSortedSuppliers()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/suppliers?sort=name&order=asc&page=1&pageSize=2",
        UserRole.Manager);

    using var response = await host.Client.SendAsync(request);
    var suppliers = await ReadRequiredJsonAsync<IReadOnlyList<SupplierResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(suppliers);
    Assert.Collection(
        suppliers!,
        supplier => Assert.Equal("Atlas Packaging", supplier.Name),
        supplier => Assert.Equal("Harbour Components", supplier.Name));
  }

  [Fact]
  public async Task CreateSupplier_WhenRequestValid_ReturnsCreatedSupplier()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Post,
        "/api/suppliers",
        UserRole.Manager,
        new CreateSupplierRequest
        {
          Name = "Harbour Components",
          Email = "procurement@harbour.example",
        });

    using var response = await host.Client.SendAsync(request);
    var supplier = await ReadRequiredJsonAsync<SupplierResponse>(response);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(supplier);
    Assert.Equal("Harbour Components", supplier!.Name);
    Assert.Equal("procurement@harbour.example", supplier.Email);
  }

  [Fact]
  public async Task ProductEndpoint_WhenRoleAllowed_ReturnsSortedProducts()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/products?sort=name&order=asc&page=1&pageSize=2",
        UserRole.Manager);

    using var response = await host.Client.SendAsync(request);
    var products = await ReadRequiredJsonAsync<IReadOnlyList<ProductResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(products);
    Assert.Collection(
        products!,
        product => Assert.Equal("Cardboard Boxes", product.Name),
        product => Assert.Equal("Protective Gloves", product.Name));
  }

  [Fact]
  public async Task CreateProduct_WhenRequestValid_ReturnsCreatedProduct()
  {
    await using var host = await TestApiHost.StartAsync();

    var supplierId = Guid.NewGuid();
    using var request = CreateRoleRequest(
        HttpMethod.Post,
        "/api/products",
        UserRole.Manager,
        new CreateProductRequest
        {
          Sku = "PKG-220",
          Name = "Warehouse Tape",
          SupplierId = supplierId,
          ReorderThreshold = 25,
          UnitCost = new MoneyDto
          {
            Amount = 4.50m,
            Currency = "GBP",
          },
        });

    using var response = await host.Client.SendAsync(request);
    var product = await ReadRequiredJsonAsync<ProductResponse>(response);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(product);
    Assert.Equal("PKG-220", product!.Sku);
    Assert.Equal("Warehouse Tape", product.Name);
    Assert.Equal(supplierId, product.SupplierId);
    Assert.Equal(0, product.QuantityOnHand);
    Assert.Equal(4.50m, product.UnitCost.Amount);
  }

  [Fact]
  public async Task CustomerOrdersOpenEndpoint_WhenWarehouseRoleAllowed_ReturnsOnlyActiveOrders()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/customer-orders/open?sort=createdAt&order=desc&page=1&pageSize=10",
        UserRole.WarehouseStaff);

    using var response = await host.Client.SendAsync(request);
    var orders = await ReadRequiredJsonAsync<IReadOnlyList<CustomerOrderResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(orders);
    Assert.All(orders!, order => Assert.Contains(order.Status, [nameof(CustomerOrderStatus.Draft), nameof(CustomerOrderStatus.Confirmed)]));
    Assert.DoesNotContain(orders!, order => order.Status == nameof(CustomerOrderStatus.Cancelled));
  }

  [Fact]
  public async Task CustomerOrderEndpoint_WhenWarehouseRoleAllowed_ReturnsOrderDetails()
  {
    await using var host = await TestApiHost.StartAsync();

    var customerOrderId = Guid.NewGuid();
    using var request = CreateRoleRequest(
        HttpMethod.Get,
        $"/api/customer-orders/{customerOrderId}",
        UserRole.WarehouseStaff);

    using var response = await host.Client.SendAsync(request);
    var order = await ReadRequiredJsonAsync<CustomerOrderResponse>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(order);
    Assert.Equal(customerOrderId, order!.CustomerOrderId);
    Assert.Equal(nameof(CustomerOrderStatus.Confirmed), order.Status);
  }

  [Fact]
  public async Task PurchaseOrdersOpenEndpoint_WhenWarehouseRoleAllowed_ReturnsOnlyReceivableOrders()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/purchase-orders/open?sort=createdAt&order=desc&page=1&pageSize=10",
        UserRole.WarehouseStaff);

    using var response = await host.Client.SendAsync(request);
    var orders = await ReadRequiredJsonAsync<IReadOnlyList<PurchaseOrderResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(orders);
    Assert.All(orders!, order => Assert.Contains(order.Status, [nameof(PurchaseOrderStatus.Pending), nameof(PurchaseOrderStatus.PartiallyReceived)]));
    Assert.DoesNotContain(orders!, order => order.Status == nameof(PurchaseOrderStatus.Completed));
  }

  [Fact]
  public async Task PurchaseOrderReceiptWorkflowEndpoints_WhenWarehouseRoleAllowed_ReturnOrderAndReceipts()
  {
    await using var host = await TestApiHost.StartAsync();

    var purchaseOrderId = Guid.NewGuid();
    using var orderRequest = CreateRoleRequest(
        HttpMethod.Get,
        $"/api/purchase-orders/{purchaseOrderId}",
        UserRole.WarehouseStaff);

    using var orderResponse = await host.Client.SendAsync(orderRequest);
    var order = await ReadRequiredJsonAsync<PurchaseOrderResponse>(orderResponse);

    Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
    Assert.NotNull(order);
    Assert.Equal(purchaseOrderId, order!.PurchaseOrderId);

    using var receiptsRequest = CreateRoleRequest(
        HttpMethod.Get,
        $"/api/purchase-orders/{purchaseOrderId}/receipts",
        UserRole.WarehouseStaff);

    using var receiptsResponse = await host.Client.SendAsync(receiptsRequest);
    var receipts = await ReadRequiredJsonAsync<IReadOnlyList<GoodsReceiptResponse>>(receiptsResponse);

    Assert.Equal(HttpStatusCode.OK, receiptsResponse.StatusCode);
    var receipt = Assert.Single(receipts!);
    Assert.Equal(purchaseOrderId, receipt.PurchaseOrderId);
  }

  [Fact]
  public async Task AdjustStock_WhenRequestValid_ReturnsUpdatedStockLevel()
  {
    await using var host = await TestApiHost.StartAsync();

    var productId = Guid.NewGuid();
    using var request = CreateRoleRequest(
        HttpMethod.Post,
        $"/api/products/{productId}/adjust-stock",
        UserRole.WarehouseStaff,
        new ContractAdjustStockRequest
        {
          Quantity = 5,
          Reason = "Cycle count correction",
        });

    using var response = await host.Client.SendAsync(request);
    var stockLevel = await ReadRequiredJsonAsync<StockLevelResponse>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(stockLevel);
    Assert.Equal(productId, stockLevel!.ProductId);
    Assert.Equal(25, stockLevel.QuantityOnHand);
  }

  [Fact]
  public async Task FinanceEndpoint_WhenRoleAllowed_ReturnsSortedTransactions()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/transactions?sort=occurredAt&order=desc&page=1&pageSize=2",
        UserRole.Administrator);

    using var response = await host.Client.SendAsync(request);
    var transactions = await ReadRequiredJsonAsync<IReadOnlyList<FinancialTransactionResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(transactions);
    Assert.Collection(
        transactions!,
        transaction =>
        {
          Assert.Equal("Posted", transaction.Status);
          Assert.Equal(480.00m, transaction.Amount.Amount);
        },
        transaction =>
        {
          Assert.Equal("Pending", transaction.Status);
          Assert.Equal(215.00m, transaction.Amount.Amount);
        });
  }

  [Fact]
  public async Task VoidOrReverseTransaction_WhenRequestValid_ReturnsUpdatedTransaction()
  {
    await using var host = await TestApiHost.StartAsync();

    var transactionId = Guid.NewGuid();
    using var request = CreateRoleRequest(
        HttpMethod.Post,
        $"/api/transactions/{transactionId}/void-or-reverse",
        UserRole.Administrator,
        new ContractVoidOrReverseTransactionRequest
        {
          Action = "Void",
          Reason = "Duplicate posting",
        });

    using var response = await host.Client.SendAsync(request);
    var transaction = await ReadRequiredJsonAsync<FinancialTransactionResponse>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(transaction);
    Assert.Equal(transactionId, transaction!.TransactionId);
    Assert.Equal("Voided", transaction.Status);
  }

  [Fact]
  public async Task GenerateFinancialReport_WhenRoleAllowed_ReturnsSummary()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/reports/financial?from=2026-03-01&to=2026-03-31",
        UserRole.Administrator);

    using var response = await host.Client.SendAsync(request);
    var report = await ReadRequiredJsonAsync<FinancialReportResponse>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(report);
    Assert.Equal(new DateOnly(2026, 3, 1), report!.From);
    Assert.Equal(new DateOnly(2026, 3, 31), report.To);
    Assert.Equal(1200.00m, report.TotalSales.Amount);
    Assert.Equal(450.00m, report.TotalExpenses.Amount);
  }

  [Fact]
  public async Task ExportFinancialReport_WhenRequestValid_ReturnsExportRecord()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Post,
        "/api/reports/financial/export",
        UserRole.Administrator,
        new ContractExportFinancialReportRequest
        {
          Format = "JSON",
          From = new DateOnly(2026, 3, 1),
          To = new DateOnly(2026, 3, 31),
        });

    using var response = await host.Client.SendAsync(request);
    var export = await ReadRequiredJsonAsync<ReportExportResponse>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(export);
    Assert.Equal("JSON", export!.Format);
    Assert.EndsWith("financial-report.json", export.FilePath, StringComparison.Ordinal);
  }

  [Fact]
  public async Task ReportExportsEndpoint_WhenRoleAllowed_ReturnsSortedExports()
  {
    await using var host = await TestApiHost.StartAsync();

    using var request = CreateRoleRequest(
        HttpMethod.Get,
        "/api/reports/exports?sort=generatedAt&order=desc&page=1&pageSize=1",
        UserRole.Administrator);

    using var response = await host.Client.SendAsync(request);
    var exports = await ReadRequiredJsonAsync<IReadOnlyList<ReportExportResponse>>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(exports);
    var export = Assert.Single(exports!);
    Assert.Equal("JSON", export.Format);
    Assert.Equal("FinancialSummary", export.ReportType);
  }

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private static HttpRequestMessage CreateRoleRequest(
      HttpMethod method,
      string requestUri,
      UserRole role,
      object? body = null)
  {
    var request = new HttpRequestMessage(method, requestUri);
    request.Headers.Add("X-Wms-Role", role.ToString());

    if (body is not null)
    {
      request.Content = JsonContent.Create(body);
    }

    return request;
  }

  private static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response)
  {
    var content = await response.Content.ReadAsStringAsync();

    try
    {
      return JsonSerializer.Deserialize<T>(content, JsonOptions)
          ?? throw new Xunit.Sdk.XunitException("Response body was empty.");
    }
    catch (JsonException exception)
    {
      throw new Xunit.Sdk.XunitException(
          $"Expected JSON but received status {(int)response.StatusCode} with body:{Environment.NewLine}{content}",
          exception);
    }
  }

  private sealed class TestApiHost : IAsyncDisposable
  {
    private TestApiHost(WebApplication app, HttpClient client)
    {
      this.App = app;
      this.Client = client;
    }

    public WebApplication App { get; }

    public HttpClient Client { get; }

    public static async Task<TestApiHost> StartAsync()
    {
      var builder = WebApplication.CreateBuilder(new WebApplicationOptions
      {
        EnvironmentName = Environments.Development,
      });

      builder.WebHost.UseUrls("http://127.0.0.1:0");
      builder.Logging.ClearProviders();
      builder.Services.Configure<WmsRoleOptions>(options =>
      {
        options.HeaderName = "X-Wms-Role";
        options.DefaultRole = null;
      });
      builder.Services.ConfigureHttpJsonOptions(options =>
      {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
      });
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(options =>
      {
        options.SupportNonNullableReferenceTypes();
        options.OperationFilter<WmsOpenApiOperationFilter>();
        options.SchemaFilter<OpenApiAllowedValuesSchemaFilter>();
        options.SwaggerDoc("v1", new OpenApiInfo
        {
          Title = "Warehouse Management API",
          Version = "v1",
        });
      });
      builder.Services.AddSingleton<IInventoryService>(new StubInventoryService());
      builder.Services.AddSingleton<ISupplierService>(new StubSupplierService());
      builder.Services.AddSingleton<IPurchaseOrderService>(new StubPurchaseOrderService());
      builder.Services.AddSingleton<IOrderService>(new StubOrderService());
      builder.Services.AddSingleton<IFinanceService>(new StubFinanceService());
      builder.Services.AddSingleton<IReportingService>(new StubReportingService());

      var app = builder.Build();

      app.UseMiddleware<ApiExceptionHandlingMiddleware>();
      app.UseMiddleware<WmsRoleAuthorizationMiddleware>();
      app.UseSwagger();
      app.MapWmsApi();

      await app.StartAsync();

      var addresses = app.Services
          .GetRequiredService<IServer>()
          .Features
          .Get<IServerAddressesFeature>()!
          .Addresses;

      var client = new HttpClient
      {
        BaseAddress = new Uri(addresses.Single()),
      };

      return new TestApiHost(app, client);
    }

    public async ValueTask DisposeAsync()
    {
      this.Client.Dispose();
      await this.App.StopAsync();
      await this.App.DisposeAsync();
    }
  }

  private sealed class StubInventoryService : IInventoryService
  {
    public Task<ProductResult> CreateProductAsync(ProductWriteModel model, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new ProductResult(
          Guid.NewGuid(),
          model.Sku,
          model.Name,
          model.SupplierId,
          model.ReorderThreshold,
          0,
          model.UnitCost));
    }

    public Task<IReadOnlyList<ProductResult>> GetProductsAsync(
        Guid? supplierId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<ProductResult>>(
          [
            new ProductResult(Guid.NewGuid(), "STL-104", "Steel Bolts Pack", Guid.NewGuid(), 60, 142, new MoneyModel(12.50m, "GBP")),
            new ProductResult(Guid.NewGuid(), "PKG-120", "Cardboard Boxes", Guid.NewGuid(), 40, 58, new MoneyModel(2.40m, "GBP")),
            new ProductResult(Guid.NewGuid(), "SAF-011", "Protective Gloves", Guid.NewGuid(), 30, 87, new MoneyModel(6.20m, "GBP")),
          ]);
    }

    public Task<ProductResult> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new ProductResult(
          productId,
          "STL-104",
          "Steel Bolts Pack",
          Guid.NewGuid(),
          60,
          142,
          new MoneyModel(12.50m, "GBP")));
    }

    public Task<ProductResult> UpdateProductAsync(Guid productId, ProductWriteModel model, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<StockLevelResult>> GetStockLevelsAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<StockLevelResult>>(
          [new StockLevelResult(Guid.NewGuid(), "SKU-001", "Warehouse Widget", 12)]);
    }

    public Task<IReadOnlyList<ProductResult>> GetLowStockProductsAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<ProductResult>>(
          [
            new ProductResult(Guid.NewGuid(), "PKG-220", "Warehouse Tape", Guid.NewGuid(), 40, 34, new MoneyModel(4.50m, "GBP")),
            new ProductResult(Guid.NewGuid(), "SAF-032", "Safety Glasses", Guid.NewGuid(), 25, 20, new MoneyModel(8.00m, "GBP")),
          ]);
    }

    public Task<StockLevelResult> AdjustStockAsync(
        Guid productId,
        Wms.Application.Inventory.AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new StockLevelResult(
          productId,
          "SKU-ADJ",
          "Adjusted Widget",
          20 + request.Quantity));
    }
  }

  private sealed class StubSupplierService : ISupplierService
  {
    public Task<SupplierResult> CreateSupplierAsync(SupplierWriteModel model, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new SupplierResult(
          Guid.NewGuid(),
          model.Name,
          model.Email,
          model.Phone,
          model.Address));
    }

    public Task<IReadOnlyList<SupplierResult>> GetSuppliersAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<SupplierResult>>(
          [
            new SupplierResult(Guid.NewGuid(), "Northfield Parts", "team@northfield.example", "0161 555 0132", "Leeds"),
            new SupplierResult(Guid.NewGuid(), "Atlas Packaging", "sales@atlas.example", "0114 555 0147", "Sheffield"),
            new SupplierResult(Guid.NewGuid(), "Harbour Components", "procurement@harbour.example", "0191 555 0188", "Newcastle"),
          ]);
    }

    public Task<SupplierResult> GetSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<SupplierResult> UpdateSupplierAsync(Guid supplierId, SupplierWriteModel model, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task DeleteSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<PurchaseOrderResult>> GetSupplierPurchaseOrdersAsync(
        Guid supplierId,
        PurchaseOrderStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }
  }

  private sealed class StubPurchaseOrderService : IPurchaseOrderService
  {
    private static readonly Guid DefaultSupplierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly PurchaseOrderLineResult DefaultLine = new(
        DefaultProductId,
        4,
        new MoneyModel(12.50m, "GBP"));
    private static readonly GoodsReceiptLineResult DefaultReceiptLine = new(DefaultProductId, 2);

    public Task<PurchaseOrderResult> CreatePurchaseOrderAsync(
        Wms.Application.PurchaseOrders.CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<PurchaseOrderResult>> GetPurchaseOrdersAsync(
        Guid? supplierId = null,
        PurchaseOrderStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      IReadOnlyList<PurchaseOrderResult> orders =
      [
        CreatePurchaseOrderResult(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), PurchaseOrderStatus.Pending, new DateTime(2026, 4, 4, 9, 0, 0, DateTimeKind.Utc)),
        CreatePurchaseOrderResult(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), PurchaseOrderStatus.PartiallyReceived, new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc)),
        CreatePurchaseOrderResult(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), PurchaseOrderStatus.Completed, new DateTime(2026, 4, 2, 11, 0, 0, DateTimeKind.Utc)),
      ];

      var filtered = orders.Where(order =>
          (!supplierId.HasValue || order.SupplierId == supplierId.Value) &&
          (!status.HasValue || order.Status == status.Value));

      return Task.FromResult<IReadOnlyList<PurchaseOrderResult>>(filtered.ToArray());
    }

    public Task<PurchaseOrderResult> GetPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(CreatePurchaseOrderResult(
          purchaseOrderId,
          PurchaseOrderStatus.Pending,
          new DateTime(2026, 4, 4, 9, 0, 0, DateTimeKind.Utc)));
    }

    public Task<PurchaseOrderResult> CancelPurchaseOrderAsync(
        Guid purchaseOrderId,
        Wms.Application.PurchaseOrders.CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<GoodsReceiptResult> ReceiveDeliveryAsync(
        Guid purchaseOrderId,
        Wms.Application.PurchaseOrders.ReceiveDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<GoodsReceiptResult>> GetReceiptsAsync(
        Guid purchaseOrderId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<GoodsReceiptResult>>(
      [
        new GoodsReceiptResult(
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            purchaseOrderId,
            new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc),
            [DefaultReceiptLine]),
      ]);
    }

    private static PurchaseOrderResult CreatePurchaseOrderResult(
        Guid purchaseOrderId,
        PurchaseOrderStatus status,
        DateTime createdAt)
    {
      return new PurchaseOrderResult(
          purchaseOrderId,
          DefaultSupplierId,
          status,
          createdAt,
          [DefaultLine],
          new MoneyModel(50.00m, "GBP"));
    }
  }

  private sealed class StubOrderService : IOrderService
  {
    public Task<CustomerOrderResult> CreateCustomerOrderAsync(
        Wms.Application.Orders.CreateCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<CustomerOrderResult>> GetCustomerOrdersAsync(
        Guid? customerId = null,
        CustomerOrderStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      IReadOnlyList<CustomerOrderResult> orders =
      [
        CreateCustomerOrderResult(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), CustomerOrderStatus.Confirmed, new DateTime(2026, 4, 4, 14, 0, 0, DateTimeKind.Utc)),
        CreateCustomerOrderResult(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), CustomerOrderStatus.Cancelled, new DateTime(2026, 4, 1, 14, 0, 0, DateTimeKind.Utc)),
      ];

      var filtered = orders.Where(order =>
          (!customerId.HasValue || order.CustomerId == customerId.Value) &&
          (!status.HasValue || order.Status == status.Value));

      return Task.FromResult<IReadOnlyList<CustomerOrderResult>>(filtered.ToArray());
    }

    public Task<CustomerOrderResult> GetCustomerOrderAsync(Guid customerOrderId, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(CreateCustomerOrderResult(
          customerOrderId,
          CustomerOrderStatus.Confirmed,
          new DateTime(2026, 4, 4, 14, 0, 0, DateTimeKind.Utc)));
    }

    public Task<CustomerOrderResult> CancelCustomerOrderAsync(
        Guid customerOrderId,
        Wms.Application.Orders.CancelCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    private static CustomerOrderResult CreateCustomerOrderResult(
        Guid customerOrderId,
        CustomerOrderStatus status,
        DateTime createdAt)
    {
      return new CustomerOrderResult(
          customerOrderId,
          Guid.Parse("99999999-9999-9999-9999-999999999999"),
          status,
          createdAt,
          [new CustomerOrderLineResult(Guid.Parse("88888888-8888-8888-8888-888888888888"), 3, new MoneyModel(8.00m, "GBP"))],
          new MoneyModel(24.00m, "GBP"));
    }
  }

  private sealed class StubFinanceService : IFinanceService
  {
    public Task<IReadOnlyList<FinancialTransactionResult>> GetTransactionsAsync(
        FinancialTransactionType? type = null,
        FinancialTransactionStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<FinancialTransactionResult>>(
          [
            new FinancialTransactionResult(
                Guid.NewGuid(),
                FinancialTransactionType.Sale,
                FinancialTransactionStatus.Pending,
                new MoneyModel(215.00m, "GBP"),
                new DateTime(2026, 3, 15, 9, 30, 0, DateTimeKind.Utc),
                Wms.Domain.Enums.ReferenceType.CustomerOrder,
                Guid.NewGuid(),
                null,
                215.00m),
            new FinancialTransactionResult(
                Guid.NewGuid(),
                FinancialTransactionType.Sale,
                FinancialTransactionStatus.Posted,
                new MoneyModel(480.00m, "GBP"),
                new DateTime(2026, 3, 31, 16, 45, 0, DateTimeKind.Utc),
                Wms.Domain.Enums.ReferenceType.CustomerOrder,
                Guid.NewGuid(),
                null,
                480.00m),
            new FinancialTransactionResult(
                Guid.NewGuid(),
                FinancialTransactionType.PurchaseExpense,
                FinancialTransactionStatus.Posted,
                new MoneyModel(90.00m, "GBP"),
                new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                Wms.Domain.Enums.ReferenceType.PurchaseOrder,
                Guid.NewGuid(),
                null,
                -90.00m),
          ]);
    }

    public Task<FinancialTransactionResult> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<FinancialTransactionResult> VoidOrReverseTransactionAsync(
        Guid transactionId,
        Wms.Application.Finance.VoidOrReverseTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
      var status = request.Action == TransactionAction.Void
          ? FinancialTransactionStatus.Voided
          : FinancialTransactionStatus.Reversed;
      Guid? reversalOfTransactionId = request.Action == TransactionAction.Reverse
          ? Guid.NewGuid()
          : null;

      return Task.FromResult(new FinancialTransactionResult(
          transactionId,
          FinancialTransactionType.Sale,
          status,
          new MoneyModel(480.00m, "GBP"),
          new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
          Wms.Domain.Enums.ReferenceType.CustomerOrder,
          Guid.NewGuid(),
          reversalOfTransactionId,
          480.00m));
    }
  }

  private sealed class StubReportingService : IReportingService
  {
    public Task<FinancialReportResult> GenerateFinancialReportAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new FinancialReportResult(
          from,
          to,
          new MoneyModel(1200.00m, "GBP"),
          new MoneyModel(450.00m, "GBP")));
    }

    public Task<ReportExportResult> ExportFinancialReportAsync(
        Wms.Application.Reporting.ExportFinancialReportRequest request,
        CancellationToken cancellationToken = default)
    {
      var filePath = request.Format == ReportFormat.JSON
          ? "/tmp/financial-report.json"
          : "/tmp/financial-report.txt";

      return Task.FromResult(new ReportExportResult(
          Guid.NewGuid(),
          ReportType.FinancialSummary,
          request.Format,
          new DateTime(2026, 4, 1, 9, 15, 0, DateTimeKind.Utc),
          filePath,
          request.From,
          request.To));
    }

    public Task<IReadOnlyList<ReportExportResult>> GetReportExportsAsync(
        ReportType? reportType = null,
        ReportFormat? format = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<ReportExportResult>>(
          [
            new ReportExportResult(
                Guid.NewGuid(),
                ReportType.FinancialSummary,
                ReportFormat.TXT,
                new DateTime(2026, 3, 31, 16, 5, 0, DateTimeKind.Utc),
                "/tmp/financial-report.txt",
                from,
                to),
            new ReportExportResult(
                Guid.NewGuid(),
                ReportType.FinancialSummary,
                ReportFormat.JSON,
                new DateTime(2026, 4, 1, 9, 15, 0, DateTimeKind.Utc),
                "/tmp/financial-report.json",
                from,
                to),
          ]);
    }
  }
}
