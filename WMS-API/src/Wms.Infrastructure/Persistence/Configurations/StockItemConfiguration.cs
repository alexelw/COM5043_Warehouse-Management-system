using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(item => item.Sku)
            .IsUnique();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.QuantityOnHand)
            .HasColumnName("quantity_on_hand")
            .IsRequired();

        builder.Property(item => item.ReorderLevel)
            .HasColumnName("reorder_level")
            .IsRequired();

        // Derived from quantity/reorder-level and not persisted as a column.
        builder.Ignore(item => item.IsLowStock);
    }
}
