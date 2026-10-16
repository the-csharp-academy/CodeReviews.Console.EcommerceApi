using EcommerceApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EcommerceApi.Tests;

/// <summary>
/// Real HTTP against the real pipeline, backed by an in-memory SQLite database that keeps the real
/// provider (unlike the InMemory provider). The connection stays open for the fixture's lifetime,
/// otherwise the schema disappears.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EcommerceDbContext>>();
            services.RemoveAll<EcommerceDbContext>();
            services.AddDbContext<EcommerceDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EcommerceDbContext>();
        db.Database.EnsureCreated();   // nothing runs migrations against :memory:
        DbSeeder.SeedAsync(db).GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

internal static class TestJson
{
    public static readonly System.Text.Json.JsonSerializerOptions Options =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    public static async Task<string> ErrorAsync(HttpResponseMessage response)
    {
        var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("error").GetString() ?? string.Empty;
    }

    public static async Task<System.Text.Json.JsonElement> BodyAsync(HttpResponseMessage response)
    {
        var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
