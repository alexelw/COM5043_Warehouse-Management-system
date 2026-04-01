using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
  public void Configure(EntityTypeBuilder<StockMovement> builder)
  {
    builder.ToTable("stock_movements");

    builder.HasKey(stockMovement => stockMovement.StockMovementId);

    builder.Property(stockMovement => stockMovement.StockMovementId)
        .HasColumnName("stock_movement_id");

    builder.Property(stockMovement => stockMovement.ProductId)
        .HasColumnName("product_id")
        .IsRequired();

    builder.HasOne<Product>()
        .WithMany()
        .HasForeignKey(stockMovement => stockMovement.ProductId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(stockMovement => stockMovement.Type)
        .HasColumnName("type")
        .IsRequired();

    builder.Property(stockMovement => stockMovement.Quantity)
        .HasColumnName("quantity")
        .IsRequired();

    builder.Property(stockMovement => stockMovement.OccurredAt)
        .HasColumnName("occurred_at")
        .IsRequired();

    builder.Property(stockMovement => stockMovement.ReferenceType)
        .HasColumnName("reference_type")
        .IsRequired();

    builder.Property(stockMovement => stockMovement.ReferenceId)
        .HasColumnName("reference_id")
        .IsRequired();

    builder.Property(stockMovement => stockMovement.Reason)
        .HasColumnName("reason")
        .HasMaxLength(256);

    builder.HasIndex(stockMovement => new { stockMovement.ProductId, stockMovement.OccurredAt });
  }
}
