using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class BasicEntityTests
{
  [Fact]
  public void Supplier_WhenNameIsBlank_ThrowsDomainRuleViolationException()
  {
    var contact = new ContactDetails("supplier@example.com", null, null);

    var action = () => new Supplier(" ", contact);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Customer_UpdateDetails_TrimsNameAndStoresContact()
  {
    var customer = new Customer("Initial");
    var contact = new ContactDetails("customer@example.com", null, null);

    customer.UpdateDetails(" Updated Customer ", contact);

    Assert.Equal("Updated Customer", customer.Name);
    Assert.Equal(contact, customer.Contact);
  }

  [Fact]
  public void User_WhenRoleIsInvalid_ThrowsDomainRuleViolationException()
  {
    var action = () => new User("Alex", (UserRole)999);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void ReportExport_WhenFilePathIsBlank_ThrowsDomainRuleViolationException()
  {
    var action = () => new ReportExport(ReportType.SalesSummary, ReportFormat.JSON, " ");

    Assert.Throws<DomainRuleViolationException>(action);
  }
}
