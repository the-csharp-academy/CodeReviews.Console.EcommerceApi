using Ecommerce.WebAPIs;
using Ecommerce.WebAPIs.Entities;
using Ecommerce.WebAPIs.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISaleService, SaleService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ArgumentException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message })
        );
    }
    catch (Exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error" })
        );
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


// Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new CategoryEntity { Name = "Shoes" },
            new CategoryEntity { Name = "T-Shirts" },
            new CategoryEntity { Name = "accessories" }
        );
    }

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new ProductEntity { Name = "Crosses black", Price = 5999, CategoryId = 1, Quantity = 10 },
            new ProductEntity { Name = "T-Shirt green", Price = 999, CategoryId = 2, Quantity = 20 },
            new ProductEntity { Name = "Hat yellow", Price = 1500, CategoryId = 3, Quantity = 5 }
        );
    }

    if (!db.Sales.Any())
    {
        var sale = new SaleEntity
        {
            SaleTime = DateTime.Now,
            Products = db.Products.Take(2).ToList()
        };
        db.Sales.Add(sale);
    }

    db.SaveChanges();
}


app.Run();
