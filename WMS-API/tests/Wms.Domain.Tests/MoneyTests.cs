using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class MoneyTests
{
  [Fact]
  public void Constructor_WhenCurrencyIsLowerCaseGbp_NormalizesCurrencyAndRoundsAmount()
  {
    var money = new Money(10.125m, "gbp");

    Assert.Equal(10.13m, money.Amount);
    Assert.Equal(Money.GbpCurrencyCode, money.Currency);
  }

  [Fact]
  public void Constructor_WhenAmountIsNegative_ThrowsDomainRuleViolationException()
  {
    var action = () => new Money(-0.01m);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Multiply_WhenMultiplierIsNegative_ThrowsDomainRuleViolationException()
  {
    var money = new Money(10m);

    var action = () => money.Multiply(-1);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Subtract_WhenResultWouldBeNegative_ThrowsDomainRuleViolationException()
  {
    var left = new Money(5m);
    var right = new Money(6m);

    var action = () => left.Subtract(right);

    Assert.Throws<DomainRuleViolationException>(action);
  }
}
