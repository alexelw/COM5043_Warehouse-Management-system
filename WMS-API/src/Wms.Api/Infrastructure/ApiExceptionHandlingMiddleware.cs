namespace Wms.Api.Infrastructure
{
  using System.Text.Json;
  using Microsoft.EntityFrameworkCore;
  using Wms.Application.Common.Exceptions;
  using Wms.Contracts.Common;
  using Wms.Domain.Exceptions;

  internal sealed class ApiExceptionHandlingMiddleware
  {
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyErrors =
        new Dictionary<string, IReadOnlyList<string>>();

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
      this._next = next;
      this._logger = logger;
      this._environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      try
      {
        await this._next(context);
      }
      catch (Exception exception)
      {
        await this.HandleExceptionAsync(context, exception);
      }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
      if (context.Response.HasStarted)
      {
        throw exception;
      }

      var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
      var (statusCode, code, message, errors) = exception switch
      {
        RequestValidationException validationException => (
            StatusCodes.Status400BadRequest,
            "validation_failed",
            validationException.Message,
            validationException.Errors),
        RoleAccessDeniedException roleAccessDeniedException => (
            StatusCodes.Status403Forbidden,
            "forbidden",
            roleAccessDeniedException.Message,
            EmptyErrors),
        ValidationException validationException => (
            StatusCodes.Status400BadRequest,
            "validation_failed",
            validationException.Message,
            EmptyErrors),
        NotFoundException notFoundException => (
            StatusCodes.Status404NotFound,
            "not_found",
            notFoundException.Message,
            EmptyErrors),
        ConflictException conflictException => (
            StatusCodes.Status409Conflict,
            "conflict",
            conflictException.Message,
            EmptyErrors),
        InvalidStatusTransitionException statusTransitionException => (
            StatusCodes.Status409Conflict,
            "conflict",
            statusTransitionException.Message,
            EmptyErrors),
        InsufficientStockException insufficientStockException => (
            StatusCodes.Status409Conflict,
            "conflict",
            insufficientStockException.Message,
            EmptyErrors),
        DomainRuleViolationException domainRuleViolationException => (
            StatusCodes.Status409Conflict,
            "conflict",
            domainRuleViolationException.Message,
            EmptyErrors),
        DbUpdateException dbUpdateException => (
            StatusCodes.Status409Conflict,
            "conflict",
            this.GetDbUpdateMessage(dbUpdateException),
            EmptyErrors),
        BadHttpRequestException badHttpRequestException => (
            StatusCodes.Status400BadRequest,
            "validation_failed",
            badHttpRequestException.Message,
            EmptyErrors),
        JsonException => (
            StatusCodes.Status400BadRequest,
            "validation_failed",
            "Request body is not valid JSON.",
            EmptyErrors),
        ArgumentException argumentException => (
            StatusCodes.Status400BadRequest,
            "validation_failed",
            argumentException.Message,
            EmptyErrors),
        _ => (
            StatusCodes.Status500InternalServerError,
            "server_error",
            "An unexpected server error occurred.",
            EmptyErrors),
      };

      if (statusCode >= StatusCodes.Status500InternalServerError)
      {
        this._logger.LogError(exception, "[WMS Error] [{TraceId}] {Message}", traceId, exception.Message);
      }
      else
      {
        this._logger.LogWarning(exception, "[WMS Error] [{TraceId}] {Message}", traceId, exception.Message);
      }

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";

      var response = new ErrorResponse
      {
        TraceId = traceId,
        Code = code,
        Message = message,
        Errors = errors,
      };

      await context.Response.WriteAsJsonAsync(response);
    }

    private string GetDbUpdateMessage(DbUpdateException exception)
    {
      if (this._environment.IsDevelopment())
      {
        return exception.InnerException?.Message ?? exception.Message;
      }

      return "The requested operation conflicts with existing data.";
    }
  }
}
