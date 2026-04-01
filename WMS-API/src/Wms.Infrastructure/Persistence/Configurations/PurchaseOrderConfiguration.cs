using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
  public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
  {
    builder.ToTable("purchase_orders");

    builder.HasKey(purchaseOrder => purchaseOrder.PurchaseOrderId);

    builder.Property(purchaseOrder => purchaseOrder.PurchaseOrderId)
        .HasColumnName("purchase_order_id");

    builder.Property(purchaseOrder => purchaseOrder.SupplierId)
        .HasColumnName("supplier_id")
        .IsRequired();

    builder.HasOne<Supplier>()
        .WithMany()
        .HasForeignKey(purchaseOrder => purchaseOrder.SupplierId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(purchaseOrder => purchaseOrder.Status)
        .HasColumnName("status")
        .IsRequired();

    builder.Property(purchaseOrder => purchaseOrder.CreatedAt)
        .HasColumnName("created_at")
        .IsRequired();

    builder.HasMany(purchaseOrder => purchaseOrder.Lines)
        .WithOne()
        .HasForeignKey(line => line.PurchaseOrderId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(purchaseOrder => purchaseOrder.Lines)
        .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(purchaseOrder => purchaseOrder.Receipts)
        .WithOne()
        .HasForeignKey(receipt => receipt.PurchaseOrderId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Navigation(purchaseOrder => purchaseOrder.Receipts)
        .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(purchaseOrder => purchaseOrder.SupplierId);

    builder.Ignore(purchaseOrder => purchaseOrder.TotalOrderedAmount);
  }
}
