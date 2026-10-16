using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceApi.Tests;

public class SalesEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    private async Task<int> CoffeeCategoryIdAsync()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/categories"));
        var coffee = body.EnumerateArray().First(c => c.GetProperty("name").GetString() == "Coffee");
        return coffee.GetProperty("id").GetInt32();
    }

    private async Task<(int Id, string Name, decimal Price)> CreateProductAsync(decimal price, int stock)
    {
        var name = UniqueName("Sale Product");
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name,
            description = "for sale tests",
            price,
            stockQuantity = stock,
            categoryId = await CoffeeCategoryIdAsync()
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        return (body.GetProperty("id").GetInt32(), name, price);
    }

    private async Task<int> StockOfAsync(int productId)
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync($"/api/products/{productId}"));
        return body.GetProperty("stockQuantity").GetInt32();
    }

    [Fact]
    public async Task GetPaged_ReturnsSeededSalesWithItems()
    {
        var response = await _client.GetAsync("/api/sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.True(body.GetProperty("totalCount").GetInt32() >= 3, $"BODY>>>{body}<<<");
        var first = body.GetProperty("items")[0];
        Assert.NotEqual(0, first.GetProperty("items").GetArrayLength());
        Assert.True(first.GetProperty("total").GetDecimal() > 0);
    }

    [Fact]
    public async Task GetPaged_PageSizeAboveMax_Returns400()
    {
        var response = await _client.GetAsync("/api/sales?pageSize=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("pageSize must be between 1 and 50.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task GetPaged_FilterByCustomer_ReturnsMatches()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/sales?customer=amara&pageSize=50"));

        var names = body.GetProperty("items").EnumerateArray()
            .Select(s => s.GetProperty("customerName").GetString() ?? string.Empty).ToList();
        Assert.NotEmpty(names);
        Assert.Contains("Amara Nkosi", names);
        Assert.All(names, name => Assert.Contains("amara", name.ToLowerInvariant()));
    }

    [Fact]
    public async Task GetPaged_FilterByDateRange_ReturnsOnlyThatRange()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/sales?from=2026-02-01&to=2026-02-08&pageSize=50"));

        var names = body.GetProperty("items").EnumerateArray()
            .Select(s => s.GetProperty("customerName").GetString()).ToList();
        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
        Assert.Contains("Amara Nkosi", names);
        Assert.Contains("Thabo Molefe", names);
        Assert.DoesNotContain("Lerato Dlamini", names);
    }

    [Fact]
    public async Task GetPaged_InvalidDateRange_Returns400()
    {
        var response = await _client.GetAsync("/api/sales?from=2026-03-01&to=2026-02-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("from must be on or before to.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task GetPaged_SortByTotalDescending_IsOrdered()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/sales?sortBy=total&sortDir=desc&pageSize=50"));

        var totals = body.GetProperty("items").EnumerateArray().Select(s => s.GetProperty("total").GetDecimal()).ToList();
        Assert.Equal(totals.OrderByDescending(t => t).ToList(), totals);
    }

    [Fact]
    public async Task GetById_ReturnsSaleWithSnapshots()
    {
        var list = await TestJson.BodyAsync(await _client.GetAsync("/api/sales?pageSize=50"));
        var id = list.GetProperty("items")[0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/sales/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.True(body.GetProperty("items").GetArrayLength() > 0);
        foreach (var item in body.GetProperty("items").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("productName").GetString()));
            Assert.True(item.GetProperty("unitPrice").GetDecimal() > 0);
            Assert.Equal(
                item.GetProperty("quantity").GetInt32() * item.GetProperty("unitPrice").GetDecimal(),
                item.GetProperty("lineTotal").GetDecimal());
        }
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/sales/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Sale 9999 was not found.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201AndComputesTotal()
    {
        var product = await CreateProductAsync(199.99m, 5);

        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Test Buyer",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.Equal(399.98m, body.GetProperty("total").GetDecimal());

        var line = body.GetProperty("items")[0];
        Assert.Equal(product.Id, line.GetProperty("productId").GetInt32());
        Assert.Equal(product.Price, line.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(product.Name, line.GetProperty("productName").GetString());
        Assert.True(body.GetProperty("id").GetInt32() > 0);
        Assert.Equal($"/api/sales/{body.GetProperty("id").GetInt32()}", response.Headers.Location!.AbsolutePath, ignoreCase: true);
    }

    [Fact]
    public async Task Create_DecrementsStock()
    {
        var product = await CreateProductAsync(50m, 5);
        Assert.Equal(5, await StockOfAsync(product.Id));

        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Stock Buyer",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(3, await StockOfAsync(product.Id));
    }

    [Fact]
    public async Task Create_InsufficientStock_Returns409()
    {
        var product = await CreateProductAsync(50m, 3);

        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Greedy Buyer",
            items = new[] { new { productId = product.Id, quantity = 10 } }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal($"Only 3 of '{product.Name}' are in stock.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_UnknownProduct_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Confused Buyer",
            items = new[] { new { productId = 9999, quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Product 9999 does not exist.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_DuplicateProductLine_Returns400()
    {
        var product = await CreateProductAsync(50m, 5);

        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Doubler",
            items = new[]
            {
                new { productId = product.Id, quantity = 1 },
                new { productId = product.Id, quantity = 1 }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Each product may appear only once in a sale.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_EmptyItems_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Empty Buyer",
            items = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A sale must contain at least one item.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_MissingCustomerName_Returns400()
    {
        var product = await CreateProductAsync(50m, 5);

        var response = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "",
            items = new[] { new { productId = product.Id, quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Customer name is required.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task UpdateSale_IsNotAllowed()
    {
        var product = await CreateProductAsync(50m, 5);
        var created = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Immutable Buyer",
            items = new[] { new { productId = product.Id, quantity = 1 } }
        });
        var id = (await TestJson.BodyAsync(created)).GetProperty("id").GetInt32();

        var response = await _client.PutAsJsonAsync($"/api/sales/{id}", new { customerName = "Someone Else" });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Delete_VoidsSaleAndRestoresStock()
    {
        var product = await CreateProductAsync(50m, 5);
        var created = await _client.PostAsJsonAsync("/api/sales", new
        {
            customerName = "Refunding Buyer",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var saleId = (await TestJson.BodyAsync(created)).GetProperty("id").GetInt32();
        Assert.Equal(3, await StockOfAsync(product.Id));

        var deleted = await _client.DeleteAsync($"/api/sales/{saleId}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(5, await StockOfAsync(product.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/sales/{saleId}")).StatusCode);

        var all = await TestJson.BodyAsync(await _client.GetAsync("/api/sales?pageSize=50"));
        Assert.DoesNotContain(all.GetProperty("items").EnumerateArray(), s => s.GetProperty("id").GetInt32() == saleId);
    }
}
