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

    var resolvedServices = new object?[]
    {
      serviceProvider.GetRequiredService<ISupplierService>(),
      serviceProvider.GetRequiredService<IInventoryService>(),
      serviceProvider.GetRequiredService<IPurchaseOrderService>(),
      serviceProvider.GetRequiredService<IOrderService>(),
      serviceProvider.GetRequiredService<IFinanceService>(),
      serviceProvider.GetRequiredService<IReportingService>(),
      serviceProvider.GetRequiredService<ISupplierRepository>(),
      serviceProvider.GetRequiredService<IProductRepository>(),
      serviceProvider.GetRequiredService<IPurchaseOrderRepository>(),
      serviceProvider.GetRequiredService<IGoodsReceiptRepository>(),
      serviceProvider.GetRequiredService<ICustomerRepository>(),
      serviceProvider.GetRequiredService<ICustomerOrderRepository>(),
      serviceProvider.GetRequiredService<IStockMovementRepository>(),
      serviceProvider.GetRequiredService<ITransactionRepository>(),
      serviceProvider.GetRequiredService<IReportExportRepository>(),
      serviceProvider.GetRequiredService<IUnitOfWork>(),
      serviceProvider.GetRequiredService<IReportExporter>(),
    };

    Assert.All(resolvedServices, Assert.NotNull);
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
