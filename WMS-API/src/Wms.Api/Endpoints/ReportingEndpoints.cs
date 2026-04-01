namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.Reporting;
  using Wms.Contracts.Reporting;
  using Wms.Domain.Enums;

  using ContractExportFinancialReportRequest = Wms.Contracts.Reporting.ExportFinancialReportRequest;

  internal static class ReportingEndpoints
  {
    private static readonly IReadOnlyDictionary<string, Func<ReportExportResult, IComparable?>> ReportExportSortSelectors =
        new Dictionary<string, Func<ReportExportResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["generatedAt"] = export => export.GeneratedAt,
        };

    public static void MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
      var reports = endpoints.MapGroup("/api/reports").WithTags("Reports");

      reports.MapGet("/financial", GenerateFinancialReportAsync)
          .RequireWmsRole(UserRole.Administrator)
          .WithWmsDocs("GenerateFinancialReport", "Generate financial report", "Generates a financial summary.")
          .Produces<FinancialReportResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      reports.MapPost("/financial/export", ExportFinancialReportAsync)
          .RequireWmsRole(UserRole.Administrator)
          .WithWmsDocs("ExportFinancialReport", "Export financial report", "Exports a financial report to file.")
          .Produces<ReportExportResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      reports.MapGet("/exports", GetReportExportsAsync)
          .RequireWmsRole(UserRole.Administrator)
          .WithWmsDocs("GetReportExports", "Get report exports", "Returns report export records.")
          .Produces<ReportExportResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GenerateFinancialReportAsync(
        string? from,
        string? to,
        IReportingService reportingService,
        CancellationToken cancellationToken)
    {
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var report = await reportingService.GenerateFinancialReportAsync(
          ApiEndpointHelpers.ToStartOfDayUtc(parsedFrom),
          ApiEndpointHelpers.ToEndOfDayUtc(parsedTo),
          cancellationToken);

      return TypedResults.Ok(report.ToResponse());
    }

    private static async Task<IResult> ExportFinancialReportAsync(
        ContractExportFinancialReportRequest request,
        IReportingService reportingService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      ApiEndpointHelpers.ValidateDateRange(request.From, request.To);

      var export = await reportingService.ExportFinancialReportAsync(
          request.ToApplicationRequest(
              ApiEndpointHelpers.ToStartOfDayUtc(request.From),
              ApiEndpointHelpers.ToEndOfDayUtc(request.To)),
          cancellationToken);

      return TypedResults.Ok(export.ToResponse());
    }

    private static async Task<IResult> GetReportExportsAsync(
        string? reportType,
        string? format,
        string? from,
        string? to,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IReportingService reportingService,
        CancellationToken cancellationToken)
    {
      var parsedReportType = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.ReportType>(reportType, "reportType");
      var parsedFormat = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.ReportFormat>(format, "format");
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var exports = await reportingService.GetReportExportsAsync(
          parsedReportType,
          parsedFormat,
          ApiEndpointHelpers.ToStartOfDayUtc(parsedFrom),
          ApiEndpointHelpers.ToEndOfDayUtc(parsedTo),
          cancellationToken);

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          exports,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "generatedAt",
          defaultDescending: true,
          ReportExportSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static export => export.ToResponse()).ToArray());
    }
  }
}
