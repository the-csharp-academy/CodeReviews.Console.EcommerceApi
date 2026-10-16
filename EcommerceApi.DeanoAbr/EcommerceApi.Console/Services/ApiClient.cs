using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceApiConsole.Models;

namespace EcommerceApiConsole.Services;

/// <summary>HTTP-only client for the Ecommerce API. No project reference to the API on purpose.</summary>
public class ApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http = new()
    {
        // The trailing slash matters: without it relative routes resolve against the last segment.
        BaseAddress = new Uri("http://localhost:5276/"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    public Task<List<Category>> GetCategoriesAsync()
        => SendAsync<List<Category>>(() => new HttpRequestMessage(HttpMethod.Get, "api/categories"));

    public Task<PagedResult<Product>> GetProductsAsync(
        int page, int pageSize, int? categoryId = null, string? search = null,
        decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null, string? sortDir = null)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (categoryId is int category) query += $"&categoryId={category}";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
        if (minPrice is decimal min) query += $"&minPrice={min}";
        if (maxPrice is decimal max) query += $"&maxPrice={max}";
        if (!string.IsNullOrWhiteSpace(sortBy)) query += $"&sortBy={Uri.EscapeDataString(sortBy)}";
        if (!string.IsNullOrWhiteSpace(sortDir)) query += $"&sortDir={Uri.EscapeDataString(sortDir)}";

        return SendAsync<PagedResult<Product>>(() => new HttpRequestMessage(HttpMethod.Get, "api/products" + query));
    }

    public Task<Product> GetProductAsync(int id)
        => SendAsync<Product>(() => new HttpRequestMessage(HttpMethod.Get, $"api/products/{id}"));

    public Task<Category> CreateCategoryAsync(CategoryRequest request)
        => SendAsync<Category>(() => new HttpRequestMessage(HttpMethod.Post, "api/categories")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        });

    public Task<Product> CreateProductAsync(ProductRequest request)
        => SendAsync<Product>(() => new HttpRequestMessage(HttpMethod.Post, "api/products")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        });

    public Task<PagedResult<Sale>> GetSalesAsync(int page, int pageSize)
        => SendAsync<PagedResult<Sale>>(() => new HttpRequestMessage(HttpMethod.Get, $"api/sales?page={page}&pageSize={pageSize}"));

    public Task<Sale> GetSaleAsync(int id)
        => SendAsync<Sale>(() => new HttpRequestMessage(HttpMethod.Get, $"api/sales/{id}"));

    public Task<Sale> CreateSaleAsync(SaleRequest request)
        => SendAsync<Sale>(() => new HttpRequestMessage(HttpMethod.Post, "api/sales")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        });

    public Task VoidSaleAsync(int id)
        => SendAsync<Sale>(() => new HttpRequestMessage(HttpMethod.Delete, $"api/sales/{id}"));

    public Task RetireProductAsync(int id)
        => SendAsync<Product>(() => new HttpRequestMessage(HttpMethod.Delete, $"api/products/{id}"));

    private async Task<T> SendAsync<T>(Func<HttpRequestMessage> createRequest)
    {
        try
        {
            using var request = createRequest();
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException($"{(int)response.StatusCode} {response.ReasonPhrase}: {await ReadErrorAsync(response)}");
            }

            if (response.StatusCode == HttpStatusCode.NoContent) return default!;

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
                   ?? throw new ApiException("The API returned an empty response body.");
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException($"Could not reach the API at {_http.BaseAddress}. Is it running? ({exception.Message})", exception);
        }
        catch (TaskCanceledException exception)
        {
            throw new ApiException($"The API at {_http.BaseAddress} did not respond in time.", exception);
        }
        catch (JsonException exception)
        {
            throw new ApiException($"The API returned a body the client could not read: {exception.Message}", exception);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return "(no detail)";

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.TryGetProperty("error", out var error)
                ? error.GetString() ?? content
                : content;
        }
        catch (JsonException)
        {
            return content;
        }
    }

    public void Dispose() => _http.Dispose();
}
