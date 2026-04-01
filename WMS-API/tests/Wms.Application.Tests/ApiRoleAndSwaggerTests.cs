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
using Wms.Contracts.Inventory;
using Wms.Domain.Enums;

namespace Wms.Application.Tests;

public sealed class ApiRoleAndSwaggerTests
{
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

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<ProductResult>> GetProductsAsync(
        Guid? supplierId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<ProductResult> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
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
      throw new NotSupportedException();
    }

    public Task<StockLevelResult> AdjustStockAsync(
        Guid productId,
        Wms.Application.Inventory.AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }
  }

  private sealed class StubSupplierService : ISupplierService
  {
    public Task<SupplierResult> CreateSupplierAsync(SupplierWriteModel model, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<SupplierResult>> GetSuppliersAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
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
      throw new NotSupportedException();
    }

    public Task<PurchaseOrderResult> GetPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<PurchaseOrderResult> CancelPurchaseOrderAsync(
        Guid purchaseOrderId,
        CancelPurchaseOrderRequest request,
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
      throw new NotSupportedException();
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
      throw new NotSupportedException();
    }

    public Task<CustomerOrderResult> GetCustomerOrderAsync(Guid customerOrderId, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<CustomerOrderResult> CancelCustomerOrderAsync(
        Guid customerOrderId,
        Wms.Application.Orders.CancelCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
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
      throw new NotSupportedException();
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
      throw new NotSupportedException();
    }
  }

  private sealed class StubReportingService : IReportingService
  {
    public Task<FinancialReportResult> GenerateFinancialReportAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<ReportExportResult> ExportFinancialReportAsync(
        Wms.Application.Reporting.ExportFinancialReportRequest request,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<ReportExportResult>> GetReportExportsAsync(
        ReportType? reportType = null,
        ReportFormat? format = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException();
    }
  }
}
