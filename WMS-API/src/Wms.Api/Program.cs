namespace Wms.Api
{
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using Microsoft.AspNetCore.Http.Json;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.OpenApi.Models;
  using Wms.Api.Endpoints;
  using Wms.Api.Infrastructure;
  using Wms.Application;
  using Wms.Infrastructure;
  using Wms.Infrastructure.Persistence;

  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddInfrastructure(builder.Configuration);
      builder.Services.AddApplication();
      builder.Services.Configure<WmsRoleOptions>(builder.Configuration.GetSection("RoleSelection"));
      builder.Services.ConfigureHttpJsonOptions(options =>
      {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
      });
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(options =>
      {
        options.SupportNonNullableReferenceTypes();
        options.OperationFilter<WmsOpenApiOperationFilter>();
        options.SchemaFilter<OpenApiAllowedValuesSchemaFilter>();
        options.SwaggerDoc("v1", new OpenApiInfo
        {
          Title = "Warehouse Management API",
          Version = "v1",
          Description = "HTTP API for supplier, inventory, order, finance, and reporting workflows.",
        });
      });

      var app = builder.Build();

      app.UseMiddleware<ApiExceptionHandlingMiddleware>();
      app.UseMiddleware<WmsRoleAuthorizationMiddleware>();
      app.UseSwagger();
      app.UseSwaggerUI();

      await ApplyMigrationsAsync(app);

      app.MapWmsApi();

      await app.RunAsync();
    }

    private static async Task ApplyMigrationsAsync(WebApplication app)
    {
      await using var scope = app.Services.CreateAsyncScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

      await dbContext.Database.MigrateAsync();
    }
  }
}
