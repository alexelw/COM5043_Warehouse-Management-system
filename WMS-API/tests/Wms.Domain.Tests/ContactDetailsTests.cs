using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class ContactDetailsTests
{
  [Fact]
  public void Constructor_WhenNoContactMethodsProvided_ThrowsDomainRuleViolationException()
  {
    var action = () => new ContactDetails(null, null, null);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Constructor_WhenEmailIsInvalid_ThrowsDomainRuleViolationException()
  {
    var action = () => new ContactDetails("invalid-email", null, null);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Constructor_WhenContactMethodsContainWhitespace_TrimsValues()
  {
    var contactDetails = new ContactDetails("  test@example.com  ", " 01234 ", " 1 Street ");

    Assert.Equal("test@example.com", contactDetails.Email);
    Assert.Equal("01234", contactDetails.Phone);
    Assert.Equal("1 Street", contactDetails.Address);
    Assert.True(contactDetails.HasAnyContactMethod);
  }
}
