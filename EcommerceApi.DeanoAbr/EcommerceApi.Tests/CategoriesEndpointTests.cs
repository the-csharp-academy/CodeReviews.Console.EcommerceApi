using System.Net;
using System.Net.Http.Json;

namespace EcommerceApi.Tests;

public class CategoriesEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsSeededCategories()
    {
        var response = await _client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.True(body.GetArrayLength() >= 5, $"BODY>>>{body}<<<");
        var names = body.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        Assert.Contains("Coffee", names);
        Assert.All(body.EnumerateArray(), c => Assert.True(c.GetProperty("productCount").GetInt32() >= 0));
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404WithErrorBody()
    {
        var response = await _client.GetAsync("/api/categories/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Category 9999 was not found.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationAndIsReadable()
    {
        var name = $"Test Category {Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/categories", new { name, description = "created by a test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal($"/api/categories/{id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var readBack = await _client.GetAsync($"/api/categories/{id}");
        Assert.Equal(HttpStatusCode.OK, readBack.StatusCode);
        Assert.Equal(name, (await TestJson.BodyAsync(readBack)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Create_MissingName_Returns400WithErrorBody()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = "", description = "no name" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category name is required.", await TestJson.ErrorAsync(response));
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var name = $"Duplicate {Guid.NewGuid():N}";
        var first = await _client.PostAsJsonAsync("/api/categories", new { name, description = "first" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/categories", new { name, description = "second" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal($"A category named '{name}' already exists.", await TestJson.ErrorAsync(second));
    }

    [Fact]
    public async Task Update_ValidRequest_RenamesCategory()
    {
        var created = await _client.PostAsJsonAsync("/api/categories", new { name = $"Rename {Guid.NewGuid():N}", description = "before" });
        var id = (await TestJson.BodyAsync(created)).GetProperty("id").GetInt32();
        var newName = $"Renamed {Guid.NewGuid():N}";

        var response = await _client.PutAsJsonAsync($"/api/categories/{id}", new { name = newName, description = "after" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestJson.BodyAsync(response);
        Assert.Equal(newName, body.GetProperty("name").GetString());
        Assert.Equal("after", body.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndHidesFromReads()
    {
        var created = await _client.PostAsJsonAsync("/api/categories", new { name = $"Delete {Guid.NewGuid():N}", description = "x" });
        var id = (await TestJson.BodyAsync(created)).GetProperty("id").GetInt32();

        var deleted = await _client.DeleteAsync($"/api/categories/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/categories/{id}")).StatusCode);
        var all = await TestJson.BodyAsync(await _client.GetAsync("/api/categories"));
        Assert.DoesNotContain(all.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Delete_CategoryWithActiveProducts_Returns409()
    {
        var created = await _client.PostAsJsonAsync("/api/categories", new { name = $"Occupied {Guid.NewGuid():N}", description = "x" });
        var categoryId = (await TestJson.BodyAsync(created)).GetProperty("id").GetInt32();

        var product = await _client.PostAsJsonAsync("/api/products", new
        {
            name = $"Product {Guid.NewGuid():N}",
            description = "blocks deletion",
            price = 10.00m,
            stockQuantity = 3,
            categoryId
        });
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);

        var deleted = await _client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        Assert.Equal($"Category {categoryId} still has 1 active products.", await TestJson.ErrorAsync(deleted));
    }
}
