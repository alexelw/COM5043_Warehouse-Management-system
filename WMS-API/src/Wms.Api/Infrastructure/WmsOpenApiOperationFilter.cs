namespace Wms.Api.Infrastructure;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Wms.Api.Endpoints;
using Wms.Domain.Enums;

internal sealed class WmsOpenApiOperationFilter : IOperationFilter
{
  private static readonly IReadOnlyDictionary<string, UserRole[]> OperationRoles =
      new Dictionary<string, UserRole[]>(StringComparer.Ordinal)
      {
        ["POST api/suppliers"] = new[] { UserRole.Manager },
        ["GET api/suppliers"] = new[] { UserRole.Manager },
        ["GET api/suppliers/{supplierId}"] = new[] { UserRole.Manager },
        ["PUT api/suppliers/{supplierId}"] = new[] { UserRole.Manager },
        ["DELETE api/suppliers/{supplierId}"] = new[] { UserRole.Manager },
        ["GET api/suppliers/{supplierId}/purchase-orders"] = new[] { UserRole.Manager },
        ["POST api/purchase-orders"] = new[] { UserRole.Manager },
        ["GET api/purchase-orders"] = new[] { UserRole.Manager },
        ["GET api/purchase-orders/{purchaseOrderId}"] = new[] { UserRole.Manager },
        ["POST api/purchase-orders/{purchaseOrderId}/cancel"] = new[] { UserRole.Manager },
        ["GET api/purchase-orders/{purchaseOrderId}/receipts"] = new[] { UserRole.Manager },
        ["GET api/products/low-stock"] = new[] { UserRole.Manager },
        ["POST api/products"] = new[] { UserRole.Manager },
        ["GET api/products"] = new[] { UserRole.Manager },
        ["GET api/products/{productId}"] = new[] { UserRole.Manager },
        ["PUT api/products/{productId}"] = new[] { UserRole.Manager },
        ["DELETE api/products/{productId}"] = new[] { UserRole.Manager },
        ["GET api/customer-orders"] = new[] { UserRole.Manager },
        ["GET api/customer-orders/{customerOrderId}"] = new[] { UserRole.Manager },
        ["POST api/purchase-orders/{purchaseOrderId}/receipts"] = new[] { UserRole.WarehouseStaff },
        ["GET api/products/stock"] = new[] { UserRole.WarehouseStaff },
        ["POST api/products/{productId}/adjust-stock"] = new[] { UserRole.WarehouseStaff },
        ["POST api/customer-orders"] = new[] { UserRole.WarehouseStaff },
        ["POST api/customer-orders/{customerOrderId}/cancel"] = new[] { UserRole.WarehouseStaff },
        ["GET api/transactions"] = new[] { UserRole.Administrator },
        ["GET api/transactions/{transactionId}"] = new[] { UserRole.Administrator },
        ["POST api/transactions/{transactionId}/void-or-reverse"] = new[] { UserRole.Administrator },
        ["GET api/reports/financial"] = new[] { UserRole.Administrator },
        ["POST api/reports/financial/export"] = new[] { UserRole.Administrator },
        ["GET api/reports/exports"] = new[] { UserRole.Administrator },
      };

  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var operationKey = BuildOperationKey(context);
    ApplyCommonParameterDocumentation(operation, operation.OperationId ?? string.Empty, operationKey);
    ApplyRoleDocumentation(operation, context, operationKey);
  }

  private static void ApplyCommonParameterDocumentation(
      OpenApiOperation operation,
      string operationId,
      string operationKey)
  {
    if (operation.Parameters is null)
    {
      return;
    }

    foreach (var parameter in operation.Parameters)
    {
      parameter.Schema ??= new OpenApiSchema();

      switch (parameter.Name)
      {
        case "page":
          parameter.Description = "1-based page index.";
          parameter.Schema.Default = new OpenApiInteger(1);
          parameter.Schema.Minimum = 1;
          break;
        case "pageSize":
          parameter.Description = "Items per page. Minimum 1, maximum 200.";
          parameter.Schema.Default = new OpenApiInteger(50);
          parameter.Schema.Minimum = 1;
          parameter.Schema.Maximum = 200;
          break;
        case "order":
          parameter.Description = "Sort direction.";
          parameter.Schema.Enum = CreateStringEnum("asc", "desc");
          break;
        case "q":
          parameter.Description = "Free-text search term.";
          break;
        case "from":
          parameter.Description = "Inclusive start date in YYYY-MM-DD format.";
          parameter.Schema.Format = "date";
          break;
        case "to":
          parameter.Description = "Inclusive end date in YYYY-MM-DD format.";
          parameter.Schema.Format = "date";
          break;
      }
    }

    switch (operationId)
    {
      case "GetPurchaseOrders":
      case "GetSupplierPurchaseOrders":
        ApplyStringEnumParameter(operation, "status", Enum.GetNames<PurchaseOrderStatus>());
        break;
      case "GetCustomerOrders":
        ApplyStringEnumParameter(operation, "status", Enum.GetNames<CustomerOrderStatus>());
        break;
      case "GetTransactions":
        ApplyStringEnumParameter(operation, "type", Enum.GetNames<FinancialTransactionType>());
        ApplyStringEnumParameter(operation, "status", Enum.GetNames<FinancialTransactionStatus>());
        break;
      case "GetReportExports":
        ApplyStringEnumParameter(operation, "reportType", Enum.GetNames<ReportType>());
        ApplyStringEnumParameter(operation, "format", Enum.GetNames<ReportFormat>());
        return;
    }

    switch (operationKey)
    {
      case "GET api/purchase-orders":
      case "GET api/suppliers/{supplierId}/purchase-orders":
        ApplyStringEnumParameter(operation, "status", Enum.GetNames<PurchaseOrderStatus>());
        break;
      case "GET api/customer-orders":
        ApplyStringEnumParameter(operation, "status", Enum.GetNames<CustomerOrderStatus>());
        break;
      case "GET api/transactions":
        ApplyStringEnumParameter(operation, "type", Enum.GetNames<FinancialTransactionType>());
        ApplyStringEnumParameter(operation, "status", Enum.GetNames<FinancialTransactionStatus>());
        break;
      case "GET api/reports/exports":
        ApplyStringEnumParameter(operation, "reportType", Enum.GetNames<ReportType>());
        ApplyStringEnumParameter(operation, "format", Enum.GetNames<ReportFormat>());
        break;
    }
  }

  private static void ApplyRoleDocumentation(OpenApiOperation operation, OperationFilterContext context, string operationKey)
  {
    var requiredRoles = context.ApiDescription.ActionDescriptor.EndpointMetadata
        .OfType<RequiredUserRoleMetadata>()
        .SingleOrDefault()
        ?.AllowedRoles
        ?? ResolveRoles(operationKey);

    if (requiredRoles is null)
    {
      return;
    }

    operation.Parameters ??= new List<OpenApiParameter>();

    if (!operation.Parameters.Any(parameter =>
            parameter.In == ParameterLocation.Header &&
            string.Equals(parameter.Name, "X-Wms-Role", StringComparison.OrdinalIgnoreCase)))
    {
      operation.Parameters.Add(new OpenApiParameter
      {
        Name = "X-Wms-Role",
        In = ParameterLocation.Header,
        Required = true,
        Description = BuildRoleHeaderDescription(requiredRoles),
        Schema = new OpenApiSchema
        {
          Type = "string",
          Enum = requiredRoles
              .Select(WmsRoleParser.ToHeaderValue)
              .Select(static value => (IOpenApiAny)new OpenApiString(value))
              .ToList(),
        },
      });
    }

    operation.Description = AppendRoleDescription(operation.Description, requiredRoles);
  }

  private static void ApplyStringEnumParameter(OpenApiOperation operation, string name, IReadOnlyList<string> values)
  {
    var parameter = operation.Parameters?.SingleOrDefault(candidate =>
        string.Equals(candidate.Name, name, StringComparison.Ordinal));
    if (parameter is null)
    {
      return;
    }

    parameter.Schema ??= new OpenApiSchema();
    parameter.Schema.Enum = values
        .Select(static value => (IOpenApiAny)new OpenApiString(value))
        .ToList();
    parameter.Description = $"Allowed values: {string.Join(", ", values)}.";
  }

  private static IList<IOpenApiAny> CreateStringEnum(params string[] values)
  {
    return values.Select(static value => (IOpenApiAny)new OpenApiString(value)).ToList();
  }

  private static string BuildRoleHeaderDescription(IReadOnlyList<UserRole> allowedRoles)
  {
    var allowedRoleNames = string.Join(", ", allowedRoles.Select(WmsRoleParser.ToDisplayName));
    return $"Select the active role for this request. Allowed roles: {allowedRoleNames}. Required unless a default role is configured server-side.";
  }

  private static string AppendRoleDescription(string? description, IReadOnlyList<UserRole> allowedRoles)
  {
    var roleSentence = allowedRoles.Count == 1
        ? $"Allowed role: {WmsRoleParser.ToDisplayName(allowedRoles[0])}."
        : $"Allowed roles: {string.Join(", ", allowedRoles.Select(WmsRoleParser.ToDisplayName))}.";

    return string.IsNullOrWhiteSpace(description)
        ? roleSentence
        : $"{description} {roleSentence}";
  }

  private static IReadOnlyList<UserRole>? ResolveRoles(string? operationKey)
  {
    if (string.IsNullOrWhiteSpace(operationKey))
    {
      return null;
    }

    return OperationRoles.TryGetValue(operationKey, out var roles)
        ? roles
        : null;
  }

  private static string BuildOperationKey(OperationFilterContext context)
  {
    var method = context.ApiDescription.HttpMethod ?? "GET";
    var relativePath = (context.ApiDescription.RelativePath ?? string.Empty)
        .Replace(":guid", string.Empty, StringComparison.OrdinalIgnoreCase)
        .TrimStart('/');

    return $"{method} {relativePath}";
  }
}
