using System.ComponentModel.DataAnnotations;
using Wms.Api.Infrastructure;

namespace Wms.Application.Tests;

public class ApiRequestValidatorTests
{
  [Fact]
  public void ValidateAndThrow_WhenNestedValidationFails_UsesCamelCaseJsonPaths()
  {
    var model = new ParentRequest
    {
      Child = new ChildRequest(),
      Items =
        [
            new ChildRequest(),
        ],
    };

    var exception = Assert.Throws<RequestValidationException>(() => ApiRequestValidator.ValidateAndThrow(model));

    Assert.Equal("The ChildId field is required.", exception.Errors["child.childId"].Single());
    Assert.Equal("The ChildId field is required.", exception.Errors["items[0].childId"].Single());
  }

  [Fact]
  public void ValidateAndThrow_WhenModelValid_DoesNotThrow()
  {
    var model = new ParentRequest
    {
      Child = new ChildRequest { ChildId = Guid.NewGuid() },
      Items =
        [
            new ChildRequest { ChildId = Guid.NewGuid() },
        ],
    };

    ApiRequestValidator.ValidateAndThrow(model);
  }

  [Fact]
  public void ValidateAndThrow_WhenPropertyContainsAcronym_UsesJsonCamelCaseNaming()
  {
    var model = new AcronymParentRequest
    {
      Item = new AcronymChildRequest(),
    };

    var exception = Assert.Throws<RequestValidationException>(() => ApiRequestValidator.ValidateAndThrow(model));

    Assert.Equal("The SKUCode field is required.", exception.Errors["item.skuCode"].Single());
  }

  private sealed class ParentRequest
  {
    [Required]
    public ChildRequest Child { get; init; } = new();

    [Required]
    public IReadOnlyList<ChildRequest> Items { get; init; } = [];
  }

  private sealed class ChildRequest
  {
    [Required]
    public Guid? ChildId { get; init; }
  }

  private sealed class AcronymParentRequest
  {
    [Required]
    public AcronymChildRequest Item { get; init; } = new();
  }

  private sealed class AcronymChildRequest
  {
    [Required]
    public string? SKUCode { get; init; }
  }
}
