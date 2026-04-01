using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
  public void Configure(EntityTypeBuilder<CustomerOrder> builder)
  {
    builder.ToTable("customer_orders");

    builder.HasKey(customerOrder => customerOrder.CustomerOrderId);

    builder.Property(customerOrder => customerOrder.CustomerOrderId)
        .HasColumnName("customer_order_id");

    builder.Property(customerOrder => customerOrder.CustomerId)
        .HasColumnName("customer_id")
        .IsRequired();

    builder.HasOne<Customer>()
        .WithMany()
        .HasForeignKey(customerOrder => customerOrder.CustomerId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(customerOrder => customerOrder.Status)
        .HasColumnName("status")
        .IsRequired();

    builder.Property(customerOrder => customerOrder.CreatedAt)
        .HasColumnName("created_at")
        .IsRequired();

    builder.HasMany(customerOrder => customerOrder.Lines)
        .WithOne()
        .HasForeignKey(line => line.CustomerOrderId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(customerOrder => customerOrder.Lines)
        .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.Ignore(customerOrder => customerOrder.TotalAmount);
  }
}
