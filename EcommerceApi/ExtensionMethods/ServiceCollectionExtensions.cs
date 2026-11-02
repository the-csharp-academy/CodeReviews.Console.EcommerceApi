using EcommerceApi.OpenApi;
using EcommerceApi.Persistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EcommerceApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, IHostApplicationBuilder builder, string connectionString)
    {
        return services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(
                builder.Configuration.GetConnectionString(connectionString) ?? throw new Exception("Can't connect to database!"),
                    sqlOpts =>
                    {
                        sqlOpts.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null
                        );
                        sqlOpts.CommandTimeout(30);
                        sqlOpts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }
            )
        );
    }
    public static IServiceCollection AddCorsWithConfigs(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        return services.AddCors(options =>
        {
            options.AddDefaultPolicy(policyBuilder =>
            {
                policyBuilder.WithOrigins(builder.Configuration["AllowedOrigins"]);
                policyBuilder.AllowAnyHeader();
                policyBuilder.AllowAnyMethod();
            });
            options.AddPolicy("AnyOrigin", policyBuilder =>
            {
                policyBuilder.AllowAnyOrigin();
                policyBuilder.AllowAnyHeader();
                policyBuilder.AllowAnyMethod();
            });
        });
    }

    public static IServiceCollection AddOpenApiWithConfigs(this IServiceCollection services)
    {
        return services.AddOpenApi(options =>
        {
            options.AddOperationTransformer(new SortColumnOperationTransformer());
            options.AddOperationTransformer(new SortOrderOperationTransformer());
        });
    }
}

