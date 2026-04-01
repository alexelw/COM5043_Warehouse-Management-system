using System.ComponentModel.DataAnnotations;
using Wms.Contracts.Common;
using Wms.Contracts.Inventory;
using Wms.Contracts.Orders;
using Wms.Contracts.PurchaseOrders;

namespace Wms.Application.Tests;

public class ContractValidationTests
{
  [Fact]
  public void CreatePurchaseOrderRequest_WhenSupplierIdMissing_IsInvalid()
  {
    var request = new CreatePurchaseOrderRequest
    {
      SupplierId = Guid.Empty,
      Lines =
        [
            new PurchaseOrderLineRequest
            {
              ProductId = Guid.NewGuid(),
              Quantity = 1,
              UnitCost = new MoneyDto { Amount = 10m, Currency = "GBP" },
            },
        ],
    };

    var results = Validate(request);

    Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.SupplierId)));
  }

  [Fact]
  public void PurchaseOrderLineRequest_WhenProductIdMissing_IsInvalid()
  {
    var request = new PurchaseOrderLineRequest
    {
      ProductId = Guid.Empty,
      Quantity = 1,
      UnitCost = new MoneyDto { Amount = 10m, Currency = "GBP" },
    };

    var results = Validate(request);

    Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.ProductId)));
  }

  [Fact]
  public void GoodsReceiptLineRequest_WhenProductIdMissing_IsInvalid()
  {
    var request = new GoodsReceiptLineRequest
    {
      ProductId = Guid.Empty,
      QuantityReceived = 1,
    };

    var results = Validate(request);

    Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.ProductId)));
  }

  [Fact]
  public void CustomerOrderLineRequest_WhenProductIdMissing_IsInvalid()
  {
    var request = new CustomerOrderLineRequest
    {
      ProductId = Guid.Empty,
      Quantity = 1,
      UnitPrice = new MoneyDto { Amount = 10m, Currency = "GBP" },
    };

    var results = Validate(request);

    Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.ProductId)));
  }

  [Fact]
  public void CreateProductRequest_WhenSupplierIdMissing_IsInvalid()
  {
    var request = new CreateProductRequest
    {
      Sku = "SKU-001",
      Name = "Widget",
      SupplierId = Guid.Empty,
      ReorderThreshold = 2,
      UnitCost = new MoneyDto { Amount = 10m, Currency = "GBP" },
    };

    var results = Validate(request);

    Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.SupplierId)));
  }

  private static IReadOnlyList<ValidationResult> Validate(object model)
  {
    var results = new List<ValidationResult>();
    _ = Validator.TryValidateObject(
        model,
        new ValidationContext(model),
        results,
        validateAllProperties: true);
    return results;
  }
}
