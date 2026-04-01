using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class FinancialTransaction
{
  private FinancialTransaction()
  {
  }

  public FinancialTransaction(
      FinancialTransactionType type,
      Money amount,
      ReferenceType referenceType,
      Guid referenceId,
      DateTime? occurredAt = null)
      : this(
          Guid.NewGuid(),
          type,
          amount,
          FinancialTransactionStatus.Pending,
          referenceType,
          referenceId,
          occurredAt ?? DateTime.UtcNow,
          null)
  {
  }

  public Guid TransactionId { get; private set; }

  public FinancialTransactionType Type { get; private set; }

  public Money Amount { get; private set; } = Money.Zero;

  public FinancialTransactionStatus Status { get; private set; }

  public DateTime OccurredAt { get; private set; }

  public ReferenceType ReferenceType { get; private set; }

  public Guid ReferenceId { get; private set; }

  public Guid? ReversalOfTransactionId { get; private set; }

  public bool IsReversal => this.ReversalOfTransactionId.HasValue;

  public decimal SignedAmount => this.IsReversal ? -this.Amount.Amount : this.Amount.Amount;

  public void MarkPosted()
  {
    if (this.Status != FinancialTransactionStatus.Pending)
    {
      throw new InvalidStatusTransitionException(
          nameof(FinancialTransaction),
          this.Status.ToString(),
          FinancialTransactionStatus.Posted.ToString());
    }

    this.Status = FinancialTransactionStatus.Posted;
  }

  public void MarkVoided()
  {
    if (this.Status is not FinancialTransactionStatus.Pending and not FinancialTransactionStatus.Posted)
    {
      throw new InvalidStatusTransitionException(
          nameof(FinancialTransaction),
          this.Status.ToString(),
          FinancialTransactionStatus.Voided.ToString());
    }

    this.Status = FinancialTransactionStatus.Voided;
  }

  public FinancialTransaction CreateReversal(DateTime? occurredAt = null)
  {
    if (this.IsReversal)
    {
      throw new DomainRuleViolationException("A reversal transaction cannot be reversed again.");
    }

    if (this.Status != FinancialTransactionStatus.Posted)
    {
      throw new InvalidStatusTransitionException(
          nameof(FinancialTransaction),
          this.Status.ToString(),
          FinancialTransactionStatus.Reversed.ToString());
    }

    this.Status = FinancialTransactionStatus.Reversed;

    return new FinancialTransaction(
        Guid.NewGuid(),
        this.Type,
        this.Amount,
        FinancialTransactionStatus.Posted,
        this.ReferenceType,
        this.ReferenceId,
        occurredAt ?? DateTime.UtcNow,
        this.TransactionId);
  }

  private FinancialTransaction(
      Guid transactionId,
      FinancialTransactionType type,
      Money amount,
      FinancialTransactionStatus status,
      ReferenceType referenceType,
      Guid referenceId,
      DateTime occurredAt,
      Guid? reversalOfTransactionId)
  {
    if (transactionId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Transaction id is required.");
    }

    if (!Enum.IsDefined(type))
    {
      throw new DomainRuleViolationException("Financial transaction type is invalid.");
    }

    ArgumentNullException.ThrowIfNull(amount);
    if (amount.IsZero)
    {
      throw new DomainRuleViolationException("Transaction amount must be greater than zero.");
    }

    if (!Enum.IsDefined(status))
    {
      throw new DomainRuleViolationException("Financial transaction status is invalid.");
    }

    if (!Enum.IsDefined(referenceType))
    {
      throw new DomainRuleViolationException("Reference type is invalid.");
    }

    if (referenceId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Reference id is required.");
    }

    this.TransactionId = transactionId;
    this.Type = type;
    this.Amount = amount;
    this.Status = status;
    this.ReferenceType = referenceType;
    this.ReferenceId = referenceId;
    this.OccurredAt = occurredAt;
    this.ReversalOfTransactionId = reversalOfTransactionId;
  }
}
