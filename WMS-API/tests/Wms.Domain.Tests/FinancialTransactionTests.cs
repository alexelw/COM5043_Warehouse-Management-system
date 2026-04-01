using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class FinancialTransactionTests
{
  [Fact]
  public void CreateReversal_WhenTransactionIsPosted_ReturnsReversalWithNegativeSignedAmount()
  {
    var transaction = new FinancialTransaction(
        FinancialTransactionType.Sale,
        new Money(75m),
        ReferenceType.CustomerOrder,
        Guid.NewGuid());

    transaction.MarkPosted();

    var reversal = transaction.CreateReversal();

    Assert.Equal(FinancialTransactionStatus.Reversed, transaction.Status);
    Assert.True(reversal.IsReversal);
    Assert.Equal(-75m, reversal.SignedAmount);
    Assert.Equal(transaction.TransactionId, reversal.ReversalOfTransactionId);
  }

  [Fact]
  public void CreateReversal_WhenTransactionIsAlreadyAReversal_ThrowsDomainRuleViolationException()
  {
    var transaction = new FinancialTransaction(
        FinancialTransactionType.Sale,
        new Money(75m),
        ReferenceType.CustomerOrder,
        Guid.NewGuid());

    transaction.MarkPosted();
    var reversal = transaction.CreateReversal();

    var action = () => reversal.CreateReversal();

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Constructor_WhenAmountIsZero_ThrowsDomainRuleViolationException()
  {
    var action = () => new FinancialTransaction(
        FinancialTransactionType.Sale,
        Money.Zero,
        ReferenceType.CustomerOrder,
        Guid.NewGuid());

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void MarkVoided_WhenTransactionIsPending_SetsStatusToVoided()
  {
    var transaction = new FinancialTransaction(
        FinancialTransactionType.PurchaseExpense,
        new Money(42m),
        ReferenceType.PurchaseOrder,
        Guid.NewGuid());

    transaction.MarkVoided();

    Assert.Equal(FinancialTransactionStatus.Voided, transaction.Status);
  }
}
