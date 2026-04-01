using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
  public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
  {
    builder.ToTable("financial_transactions");

    builder.HasKey(transaction => transaction.TransactionId);

    builder.Property(transaction => transaction.TransactionId)
        .HasColumnName("transaction_id");

    builder.Property(transaction => transaction.Type)
        .HasColumnName("type")
        .IsRequired();

    builder.OwnsOne(
        transaction => transaction.Amount,
        money =>
        {
          money.Property(value => value.Amount)
                  .HasColumnName("amount")
                  .HasPrecision(18, 2)
                  .IsRequired();

          money.Property(value => value.Currency)
                  .HasColumnName("currency")
                  .HasMaxLength(3)
                  .HasDefaultValue(Money.GbpCurrencyCode)
                  .IsRequired();
        });

    builder.Navigation(transaction => transaction.Amount)
        .IsRequired();

    builder.Property(transaction => transaction.Status)
        .HasColumnName("status")
        .IsRequired();

    builder.Property(transaction => transaction.OccurredAt)
        .HasColumnName("occurred_at")
        .IsRequired();

    builder.Property(transaction => transaction.ReferenceType)
        .HasColumnName("reference_type")
        .IsRequired();

    builder.Property(transaction => transaction.ReferenceId)
        .HasColumnName("reference_id")
        .IsRequired();

    builder.Property(transaction => transaction.ReversalOfTransactionId)
        .HasColumnName("reversal_of_transaction_id");

    builder.HasOne<FinancialTransaction>()
        .WithMany()
        .HasForeignKey(transaction => transaction.ReversalOfTransactionId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(transaction => transaction.OccurredAt);

    builder.Ignore(transaction => transaction.IsReversal);
    builder.Ignore(transaction => transaction.SignedAmount);
  }
}
