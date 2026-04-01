using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Api.Infrastructure;
using Wms.Application.Common.Exceptions;
using Wms.Contracts.Common;

namespace Wms.Application.Tests;

public sealed class ApiExceptionHandlingMiddlewareTests
{
  [Fact]
  public async Task InvokeAsync_WhenValidationFails_ReturnsStructuredBadRequest()
  {
    var middleware = CreateMiddleware(_ =>
        throw RequestValidationException.ForSingleError("page", "Page must be greater than or equal to 1."));
    var context = CreateContext();

    await middleware.InvokeAsync(context);

    var response = await ReadResponseAsync(context);

    Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.Ordinal);
    Assert.Equal("validation_failed", response.Code);
    Assert.Equal("One or more validation errors occurred.", response.Message);
    Assert.Equal("Page must be greater than or equal to 1.", response.Errors["page"].Single());
    Assert.False(string.IsNullOrWhiteSpace(response.TraceId));
  }

  [Fact]
  public async Task InvokeAsync_WhenResourceMissing_ReturnsStructuredNotFound()
  {
    var productId = Guid.NewGuid();
    var middleware = CreateMiddleware(_ => throw new NotFoundException("Product", productId));
    var context = CreateContext();

    await middleware.InvokeAsync(context);

    var response = await ReadResponseAsync(context);

    Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    Assert.Equal("not_found", response.Code);
    Assert.Equal($"Product '{productId}' was not found.", response.Message);
    Assert.Empty(response.Errors);
  }

  [Fact]
  public async Task InvokeAsync_WhenJsonParsingFails_ReturnsStructuredBadRequest()
  {
    var middleware = CreateMiddleware(_ => throw new JsonException("Bad JSON."));
    var context = CreateContext();

    await middleware.InvokeAsync(context);

    var response = await ReadResponseAsync(context);

    Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    Assert.Equal("validation_failed", response.Code);
    Assert.Equal("Request body is not valid JSON.", response.Message);
    Assert.Empty(response.Errors);
  }

  private static ApiExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
  {
    return new ApiExceptionHandlingMiddleware(next, NullLogger<ApiExceptionHandlingMiddleware>.Instance);
  }

  private static DefaultHttpContext CreateContext()
  {
    return new DefaultHttpContext
    {
      Response =
      {
        Body = new MemoryStream(),
      },
    };
  }

  private static async Task<ErrorResponse> ReadResponseAsync(HttpContext context)
  {
    context.Response.Body.Position = 0;
    return (await JsonSerializer.DeserializeAsync<ErrorResponse>(
        context.Response.Body,
        new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
  }
}
