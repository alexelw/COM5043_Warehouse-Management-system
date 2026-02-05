using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence;

public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<StockItem> StockItems => this.Set<StockItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
