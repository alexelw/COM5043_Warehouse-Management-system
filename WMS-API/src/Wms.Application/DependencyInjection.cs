using Microsoft.Extensions.DependencyInjection;
using Wms.Application.Abstractions;
using Wms.Application.Finance;
using Wms.Application.Inventory;
using Wms.Application.Orders;
using Wms.Application.PurchaseOrders;
using Wms.Application.Reporting;
using Wms.Application.Suppliers;

namespace Wms.Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.AddSingleton<IClock, SystemClock>();
    services.AddScoped<ISupplierService, SupplierService>();
    services.AddScoped<IInventoryService, InventoryService>();
    services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IFinanceService, FinanceService>();
    services.AddScoped<IReportingService, ReportingService>();
    return services;
  }
}
