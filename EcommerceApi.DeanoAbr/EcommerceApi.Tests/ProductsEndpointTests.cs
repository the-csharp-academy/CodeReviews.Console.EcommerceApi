using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceApi.Tests;

public class ProductsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    private async Task<int> CoffeeCategoryIdAsync()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/categories"));
        var coffee = body.EnumerateArray().First(c => c.GetProperty("name").GetString() == "Coffee");
        return coffee.GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> CreateProductAsync(string name, decimal price = 199.99m, int stock = 7, int? categoryId = null)
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name,
            description = "created by a test",
            price,
            stockQuantity = stock,
            categoryId = categoryId ?? await CoffeeCategoryIdAsync()
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await TestJson.BodyAsync(response);
    }

    [Fact]
    public async Task GetPaged_DefaultPage_ReturnsTenItemsWithMetadata()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.Equal(10, body.GetProperty("items").GetArrayLength());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(10, body.GetProperty("pageSize").GetInt32());
        var totalCount = body.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 12, $"totalCount was {totalCount}");
        Assert.Equal((int)Math.Ceiling(totalCount / 10.0), body.GetProperty("totalPages").GetInt32());
        Assert.False(body.GetProperty("hasPrevious").GetBoolean());
    }

    [Fact]
    public async Task GetPaged_IsSelfConsistentAcrossPages()
    {
        var all = await TestJson.BodyAsync(await _client.GetAsync("/api/products?pageSize=50"));
        var totalCount = all.GetProperty("totalCount").GetInt32();

        var secondPage = await TestJson.BodyAsync(await _client.GetAsync("/api/products?page=2&pageSize=5"));

        Assert.Equal(5, secondPage.GetProperty("items").GetArrayLength());
        Assert.Equal(totalCount, secondPage.GetProperty("totalCount").GetInt32());
        Assert.True(secondPage.GetProperty("hasPrevious").GetBoolean());
    }

    [Fact]
    public async Task GetPaged_PageZero_Returns400()
    {
        var response = await _client.GetAsync("/api/products?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("page must be at least 1.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task GetPaged_PageSizeAboveMax_Returns400()
    {
        var response = await _client.GetAsync("/api/products?pageSize=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("pageSize must be between 1 and 50.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task GetPaged_InvalidSortBy_Returns400()
    {
        var response = await _client.GetAsync("/api/products?sortBy=colour");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("sortBy must be one of: name, price, stock, createdAt.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task GetPaged_FilterByCategory_ReturnsOnlyThatCategory()
    {
        var categoryId = await CoffeeCategoryIdAsync();

        var body = await TestJson.BodyAsync(await _client.GetAsync($"/api/products?categoryId={categoryId}&pageSize=50"));

        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, p => Assert.Equal(categoryId, p.GetProperty("categoryId").GetInt32()));
    }

    [Fact]
    public async Task GetPaged_FilterByPriceRange_ReturnsOnlyThatRange()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/products?minPrice=100&maxPrice=500&pageSize=50"));

        var prices = body.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("price").GetDecimal()).ToList();
        Assert.NotEmpty(prices);
        Assert.All(prices, price => Assert.InRange(price, 100m, 500m));
    }

    [Fact]
    public async Task GetPaged_SearchIsCaseInsensitive()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/products?search=ethiopia&pageSize=50"));

        var names = body.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
        Assert.Contains("Ethiopia Yirgacheffe 250g", names);
    }

    [Fact]
    public async Task GetPaged_SortByPriceDescending_IsOrdered()
    {
        var body = await TestJson.BodyAsync(await _client.GetAsync("/api/products?sortBy=price&sortDir=desc&pageSize=50"));

        var prices = body.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("price").GetDecimal()).ToList();
        Assert.Equal(prices.OrderByDescending(p => p).ToList(), prices);
    }

    [Fact]
    public async Task GetById_ReturnsSeededProduct()
    {
        var list = await TestJson.BodyAsync(await _client.GetAsync("/api/products?search=Ethiopia Yirgacheffe&pageSize=50"));
        var id = list.GetProperty("items")[0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.Equal("Ethiopia Yirgacheffe 250g", body.GetProperty("name").GetString());
        Assert.Equal(89.50m, body.GetProperty("price").GetDecimal());
        Assert.Equal("Coffee", body.GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/products/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Product 9999 was not found.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        var name = UniqueName("Test Product");

        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name,
            description = "a fresh roast",
            price = 123.45m,
            stockQuantity = 9,
            categoryId = await CoffeeCategoryIdAsync()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal($"/api/products/{id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var readBack = await TestJson.BodyAsync(await _client.GetAsync($"/api/products/{id}"));
        Assert.Equal(name, readBack.GetProperty("name").GetString());
        Assert.Equal("Coffee", readBack.GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Create_WhitespaceName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "   ",
            description = "blank name",
            price = 10m,
            stockQuantity = 1,
            categoryId = await CoffeeCategoryIdAsync()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Product name is required.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_UnknownCategory_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = UniqueName("Orphan"),
            description = "no such category",
            price = 10m,
            stockQuantity = 1,
            categoryId = 9999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category 9999 does not exist.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_NegativePrice_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = UniqueName("Negative"),
            description = "bad price",
            price = -5m,
            stockQuantity = 1,
            categoryId = await CoffeeCategoryIdAsync()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Price must be greater than 0.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var name = UniqueName("Duplicate Product");
        await CreateProductAsync(name);

        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name,
            description = "second attempt",
            price = 10m,
            stockQuantity = 1,
            categoryId = await CoffeeCategoryIdAsync()
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal($"A product named '{name}' already exists.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task UpdatePrice_IsNotAllowed()
    {
        var created = await CreateProductAsync(UniqueName("Immutable"));

        var response = await _client.PutAsJsonAsync($"/api/products/{created.GetProperty("id").GetInt32()}", new
        {
            name = created.GetProperty("name").GetString(),
            description = "trying to change the price",
            price = 9999m,
            stockQuantity = 1,
            categoryId = created.GetProperty("categoryId").GetInt32()
        });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndHidesFromReads()
    {
        var created = await CreateProductAsync(UniqueName("Retired"));
        var id = created.GetProperty("id").GetInt32();

        var deleted = await _client.DeleteAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/products/{id}")).StatusCode);

        var all = await TestJson.BodyAsync(await _client.GetAsync("/api/products?pageSize=50"));
        Assert.DoesNotContain(all.GetProperty("items").EnumerateArray(), p => p.GetProperty("id").GetInt32() == id);
    }
}
