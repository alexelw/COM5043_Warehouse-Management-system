using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence;

public class WmsDbContext : DbContext
{
  public WmsDbContext(DbContextOptions<WmsDbContext> options)
      : base(options)
  {
  }

  public DbSet<Customer> Customers => this.Set<Customer>();

  public DbSet<CustomerOrder> CustomerOrders => this.Set<CustomerOrder>();

  public DbSet<CustomerOrderLine> CustomerOrderLines => this.Set<CustomerOrderLine>();

  public DbSet<FinancialTransaction> FinancialTransactions => this.Set<FinancialTransaction>();

  public DbSet<GoodsReceipt> GoodsReceipts => this.Set<GoodsReceipt>();

  public DbSet<GoodsReceiptLine> GoodsReceiptLines => this.Set<GoodsReceiptLine>();

  public DbSet<Product> Products => this.Set<Product>();

  public DbSet<PurchaseOrder> PurchaseOrders => this.Set<PurchaseOrder>();

  public DbSet<PurchaseOrderLine> PurchaseOrderLines => this.Set<PurchaseOrderLine>();

  public DbSet<ReportExport> ReportExports => this.Set<ReportExport>();

  public DbSet<StockMovement> StockMovements => this.Set<StockMovement>();

  public DbSet<Supplier> Suppliers => this.Set<Supplier>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
  }
}
