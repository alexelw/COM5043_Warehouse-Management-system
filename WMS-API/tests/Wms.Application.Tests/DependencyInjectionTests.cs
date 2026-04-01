using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Application;
using Wms.Application.Abstractions;
using Wms.Application.Finance;
using Wms.Application.Inventory;
using Wms.Application.Orders;
using Wms.Application.PurchaseOrders;
using Wms.Application.Reporting;
using Wms.Application.Suppliers;
using Wms.Domain.Repositories;
using Wms.Infrastructure;

namespace Wms.Application.Tests;

public class DependencyInjectionTests
{
  [Fact]
  public void AddApplicationAndInfrastructure_RegistersResolvableRuntimeServices()
  {
    var services = new ServiceCollection();
    services.AddApplication();
    services.AddInfrastructure(BuildConfiguration());

    using var provider = services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateOnBuild = true,
      ValidateScopes = true,
    });
    using var scope = provider.CreateScope();
    var serviceProvider = scope.ServiceProvider;

    _ = serviceProvider.GetRequiredService<ISupplierService>();
    _ = serviceProvider.GetRequiredService<IInventoryService>();
    _ = serviceProvider.GetRequiredService<IPurchaseOrderService>();
    _ = serviceProvider.GetRequiredService<IOrderService>();
    _ = serviceProvider.GetRequiredService<IFinanceService>();
    _ = serviceProvider.GetRequiredService<IReportingService>();
    _ = serviceProvider.GetRequiredService<ISupplierRepository>();
    _ = serviceProvider.GetRequiredService<IProductRepository>();
    _ = serviceProvider.GetRequiredService<IPurchaseOrderRepository>();
    _ = serviceProvider.GetRequiredService<IGoodsReceiptRepository>();
    _ = serviceProvider.GetRequiredService<ICustomerRepository>();
    _ = serviceProvider.GetRequiredService<ICustomerOrderRepository>();
    _ = serviceProvider.GetRequiredService<IStockMovementRepository>();
    _ = serviceProvider.GetRequiredService<ITransactionRepository>();
    _ = serviceProvider.GetRequiredService<IReportExportRepository>();
    _ = serviceProvider.GetRequiredService<IUnitOfWork>();
    _ = serviceProvider.GetRequiredService<IReportExporter>();
  }

  private static IConfiguration BuildConfiguration()
  {
    return new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["ConnectionStrings:DefaultConnection"] =
                "server=localhost;port=3306;database=wms_db;user=wms_user;password=wms_pass;",
        })
        .Build();
  }
}
