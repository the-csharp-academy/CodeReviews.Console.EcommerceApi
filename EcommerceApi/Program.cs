using EcommerceApi.Extensions;
using EcommerceApi.Persistant;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddAppDbContext(builder, "EcommerceDb");
        builder.Services.AddCorsWithConfigs(builder);



        var app = builder.Build();

        if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
        {
            app.UseDeveloperExceptionPage();
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
                options.SwaggerEndpoint("/openapi/v1.json", "OpenApi - v1")
            );
        }
        else
        {
            app.UseExceptionHandler(action =>
            {
                action.Run(async context =>
                {
                    var exceptionHandler =
                        context.Features.Get<IExceptionHandlerPathFeature>();
                    var details = new ProblemDetails();
                    details.Detail = exceptionHandler?.Error.Message;
                    details.Extensions["traceId"] =
                    System.Diagnostics.Activity.Current?.Id
                        ?? context.TraceIdentifier;
                    details.Type =
                        "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                    details.Status = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(details));
                });

            });
        }

        app.UseHttpsRedirection();

        app.UseCors();
        app.UseAuthorization();

        app.MapControllers();

        app.MapGet("/error",
            [EnableCors("AnyOrigin")]
            [ResponseCache(NoStore = false)]
            () => Results.Problem());

        app.MapGet("/error/test",
            [EnableCors("AnyOrigin")]
            [ResponseCache(NoStore = false)]
            () => { throw new Exception(); });

        app.Run();
    }
}

