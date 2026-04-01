namespace Wms.Api
{
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using Microsoft.AspNetCore.Http.Json;
  using Microsoft.OpenApi.Models;
  using Wms.Api.Endpoints;
  using Wms.Api.Infrastructure;
  using Wms.Application;
  using Wms.Infrastructure;

  public static class Program
  {
    public static Task Main(string[] args)
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

      app.MapWmsApi();

      return app.RunAsync();
    }
  }
}
