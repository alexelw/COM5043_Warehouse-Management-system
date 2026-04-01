namespace Wms.Api.Endpoints
{
  using Wms.Api.Infrastructure;
  using Wms.Application.Finance;
  using Wms.Contracts.Finance;
  using Wms.Domain.Enums;

  using ContractVoidOrReverseTransactionRequest = Wms.Contracts.Finance.VoidOrReverseTransactionRequest;

  internal static class FinanceEndpoints
  {
    private static readonly IReadOnlyDictionary<string, Func<FinancialTransactionResult, IComparable?>> TransactionSortSelectors =
        new Dictionary<string, Func<FinancialTransactionResult, IComparable?>>(StringComparer.OrdinalIgnoreCase)
        {
          ["occurredAt"] = transaction => transaction.OccurredAt,
          ["amount"] = transaction => transaction.Amount.Amount,
        };

    public static void MapFinanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
      var group = endpoints.MapGroup("/api/transactions").WithTags("Finance");

      group.MapGet("/", GetTransactionsAsync)
          .RequireWmsRole(UserRole.Administrator)
          .WithWmsDocs("GetTransactions", "Get financial transactions", "Returns recorded financial transactions.")
          .Produces<FinancialTransactionResponse[]>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status500InternalServerError);

      group.MapGet("/{transactionId:guid}", GetTransactionAsync)
          .RequireWmsRole(UserRole.Administrator)
          .WithWmsDocs("GetTransaction", "Get financial transaction", "Returns a specific financial transaction.")
          .Produces<FinancialTransactionResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status404NotFound,
              StatusCodes.Status500InternalServerError);

      group.MapPost("/{transactionId:guid}/void-or-reverse", VoidOrReverseTransactionAsync)
          .RequireWmsRole(UserRole.Administrator)
          .WithWmsDocs(
              "VoidOrReverseTransaction",
              "Void or reverse transaction",
              "Voids a transaction or creates a reversal linked to the original transaction.")
          .Produces<FinancialTransactionResponse>(StatusCodes.Status200OK)
          .ProducesErrorResponses(
              StatusCodes.Status400BadRequest,
              StatusCodes.Status404NotFound,
              StatusCodes.Status409Conflict,
              StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetTransactionsAsync(
        string? type,
        string? status,
        string? from,
        string? to,
        string? sort,
        string? order,
        int? page,
        int? pageSize,
        IFinanceService financeService,
        CancellationToken cancellationToken)
    {
      var parsedType = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.FinancialTransactionType>(type, "type");
      var parsedStatus = ApiEndpointHelpers.ParseOptionalEnum<Wms.Domain.Enums.FinancialTransactionStatus>(status, "status");
      var parsedFrom = ApiEndpointHelpers.ParseOptionalDate(from, "from");
      var parsedTo = ApiEndpointHelpers.ParseOptionalDate(to, "to");
      ApiEndpointHelpers.ValidateDateRange(parsedFrom, parsedTo);

      var transactions = await financeService.GetTransactionsAsync(
          parsedType,
          parsedStatus,
          ApiEndpointHelpers.ToStartOfDayUtc(parsedFrom),
          ApiEndpointHelpers.ToEndOfDayUtc(parsedTo),
          cancellationToken);

      var shapedResults = ApiEndpointHelpers.ApplyListOptions(
          transactions,
          sort,
          order,
          page ?? 1,
          pageSize ?? 50,
          defaultSort: "occurredAt",
          defaultDescending: true,
          TransactionSortSelectors);

      return TypedResults.Ok(shapedResults.Select(static transaction => transaction.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetTransactionAsync(
        Guid transactionId,
        IFinanceService financeService,
        CancellationToken cancellationToken)
    {
      var transaction = await financeService.GetTransactionAsync(transactionId, cancellationToken);
      return TypedResults.Ok(transaction.ToResponse());
    }

    private static async Task<IResult> VoidOrReverseTransactionAsync(
        Guid transactionId,
        ContractVoidOrReverseTransactionRequest request,
        IFinanceService financeService,
        CancellationToken cancellationToken)
    {
      ApiRequestValidator.ValidateAndThrow(request);

      var transaction = await financeService.VoidOrReverseTransactionAsync(
          transactionId,
          request.ToApplicationRequest(),
          cancellationToken);

      return TypedResults.Ok(transaction.ToResponse());
    }
  }
}
