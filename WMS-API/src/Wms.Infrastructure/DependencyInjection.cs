using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Application.Abstractions;
using Wms.Domain.Repositories;
using Wms.Infrastructure.Persistence;
using Wms.Infrastructure.Persistence.Repositories;
using Wms.Infrastructure.Reporting;

namespace Wms.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    services.AddDbContext<WmsDbContext>(options =>
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 0)),
            mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(WmsDbContext).Assembly.FullName)));

    services.AddScoped<ISupplierRepository, SupplierRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
    services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
    services.AddScoped<ICustomerRepository, CustomerRepository>();
    services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
    services.AddScoped<IStockMovementRepository, StockMovementRepository>();
    services.AddScoped<ITransactionRepository, TransactionRepository>();
    services.AddScoped<IReportExportRepository, ReportExportRepository>();
    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    services.AddSingleton<IReportExporter, FileReportExporter>();

    return services;
  }
}
