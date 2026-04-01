namespace Wms.Api.Endpoints;

internal static class EndpointRouteBuilderExtensions
{
  public static void MapWmsApi(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapHealthEndpoints();
    endpoints.MapSupplierEndpoints();
    endpoints.MapProductEndpoints();
    endpoints.MapPurchaseOrderEndpoints();
    endpoints.MapCustomerOrderEndpoints();
    endpoints.MapFinanceEndpoints();
    endpoints.MapReportingEndpoints();
  }
}
