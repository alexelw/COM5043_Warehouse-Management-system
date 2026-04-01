using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
  public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
  {
    builder.ToTable("goods_receipts");

    builder.HasKey(receipt => receipt.GoodsReceiptId);

    builder.Property(receipt => receipt.GoodsReceiptId)
        .HasColumnName("goods_receipt_id");

    builder.Property(receipt => receipt.PurchaseOrderId)
        .HasColumnName("purchase_order_id")
        .IsRequired();

    builder.Property(receipt => receipt.ReceivedAt)
        .HasColumnName("received_at")
        .IsRequired();

    builder.HasMany(receipt => receipt.Lines)
        .WithOne()
        .HasForeignKey(line => line.GoodsReceiptId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(receipt => receipt.Lines)
        .UsePropertyAccessMode(PropertyAccessMode.Field);
  }
}
