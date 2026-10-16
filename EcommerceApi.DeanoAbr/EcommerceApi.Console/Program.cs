using EcommerceApiConsole.Models;
using EcommerceApiConsole.Presentation;
using EcommerceApiConsole.Services;

namespace EcommerceApiConsole;

public static class Program
{
    public static async Task<int> Main()
    {
        using var api = new ApiClient();

        Menu.Banner();
        while (true)
        {
            Menu.Options();
            var choice = Menu.Read("> ");

            if (Menu.InputClosed || choice is null or "" or "0")
            {
                Console.WriteLine("Bye.");
                return 0;
            }

            try
            {
                await HandleAsync(choice, api);
            }
            catch (ApiException exception)
            {
                Menu.Error(exception.Message);
            }

            if (Menu.InputClosed) return 0;
            Menu.Pause();
        }
    }

    private static async Task HandleAsync(string choice, ApiClient api)
    {
        switch (choice)
        {
            case "1": await BrowseProductsAsync(api); break;
            case "2": await SearchProductsAsync(api); break;
            case "3": await ProductDetailsAsync(api); break;
            case "4": Menu.PrintCategories(await api.GetCategoriesAsync()); break;
            case "5": await CreateCategoryAsync(api); break;
            case "6": await CreateProductAsync(api); break;
            case "7": await RecordSaleAsync(api); break;
            case "8": await BrowseSalesAsync(api); break;
            case "9": Menu.PrintSale(await api.GetSaleAsync(Menu.ReadInt("Sale id: "))); break;
            case "10": await VoidSaleAsync(api); break;
            case "11": await RetireProductAsync(api); break;
            default: Menu.Info("  Unknown option."); break;
        }
    }

    private static async Task BrowseProductsAsync(ApiClient api)
    {
        var page = 1;
        while (true)
        {
            var result = await api.GetProductsAsync(page, Menu.PageSize);
            Menu.PrintProducts(result.Items);
            Menu.PrintPagedFooter("products", result.Page, result.PageSize, result.TotalCount, result.TotalPages);

            var command = Menu.Read("[n]ext, [p]revious, [q]uit: ");
            if (Menu.InputClosed) return;
            if (command is "n" && result.HasNext) page++;
            else if (command is "p" && result.HasPrevious) page--;
            else if (command is not "n" and not "p") return;
        }
    }

    private static async Task SearchProductsAsync(ApiClient api)
    {
        var search = Menu.Read("Name contains (blank for any): ");
        var categoryId = Menu.ReadOptionalInt("Category id (blank for any): ");
        var minPrice = Menu.ReadOptionalDecimal("Minimum price (blank for any): ");
        var maxPrice = Menu.ReadOptionalDecimal("Maximum price (blank for any): ");
        var sortBy = Menu.Read("Sort by [name|price|stock|createdAt] (blank for newest): ");

        var sortDir = Menu.Read("Direction [asc|desc] (blank for default): ");

        var result = await api.GetProductsAsync(1, Menu.PageSize, categoryId, search, minPrice, maxPrice, sortBy, sortDir);
        Menu.PrintProducts(result.Items);
        Menu.PrintPagedFooter("matches", result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    private static async Task ProductDetailsAsync(ApiClient api)
        => Menu.PrintProduct(await api.GetProductAsync(Menu.ReadInt("Product id: ")));

    private static async Task CreateCategoryAsync(ApiClient api)
    {
        var name = Menu.ReadRequired("Category name: ");
        var description = Menu.Read("Description (optional): ");

        var created = await api.CreateCategoryAsync(new CategoryRequest(name, description));
        Menu.Info($"  Created category #{created.Id} '{created.Name}'.");
    }

    private static async Task CreateProductAsync(ApiClient api)
    {
        Menu.PrintCategories(await api.GetCategoriesAsync());

        var name = Menu.ReadRequired("Product name: ");
        var description = Menu.Read("Description (optional): ");
        var price = Menu.ReadDecimal("Price: ");
        var stock = Menu.ReadInt("Stock quantity (0 allowed): ", minimum: 0);
        var categoryId = Menu.ReadInt("Category id: ");

        var created = await api.CreateProductAsync(new ProductRequest(name, description, price, stock, categoryId));
        Menu.Info($"  Created product #{created.Id} '{created.Name}' at {created.Price:0.00}.");
    }

    private static async Task RecordSaleAsync(ApiClient api)
    {
        var customer = Menu.ReadRequired("Customer name: ");
        var lines = new List<SaleLineRequest>();

        while (true)
        {
            var productId = Menu.ReadOptionalInt("Product id (blank when finished): ");
            if (Menu.InputClosed) return;
            if (productId is null) break;

            var quantity = Menu.ReadInt("Quantity: ");
            lines.Add(new SaleLineRequest(productId.Value, quantity));
            Menu.Info($"  Added line {productId} x{quantity}.");
        }

        if (lines.Count == 0)
        {
            Menu.Info("  A sale needs at least one line — nothing was recorded.");
            return;
        }

        var sale = await api.CreateSaleAsync(new SaleRequest(customer, lines));
        Menu.PrintSale(sale);
    }

    private static async Task BrowseSalesAsync(ApiClient api)
    {
        var page = 1;
        while (true)
        {
            var result = await api.GetSalesAsync(page, Menu.PageSize);
            Menu.PrintSales(result.Items);
            Menu.PrintPagedFooter("sales", result.Page, result.PageSize, result.TotalCount, result.TotalPages);

            var command = Menu.Read("[n]ext, [p]revious, [q]uit: ");
            if (Menu.InputClosed) return;
            if (command is "n" && result.HasNext) page++;
            else if (command is "p" && result.HasPrevious) page--;
            else if (command is not "n" and not "p") return;
        }
    }

    private static async Task VoidSaleAsync(ApiClient api)
    {
        var id = Menu.ReadInt("Sale id to void: ");
        if (!Menu.Confirm($"Void sale #{id} and return its stock?"))
        {
            Menu.Info("  Cancelled.");
            return;
        }

        await api.VoidSaleAsync(id);
        Menu.Info($"  Sale #{id} was voided and its stock returned.");
    }

    private static async Task RetireProductAsync(ApiClient api)
    {
        var id = Menu.ReadInt("Product id to retire: ");
        Menu.PrintProduct(await api.GetProductAsync(id));

        if (!Menu.Confirm("Retire this product? Past sales keep their prices."))
        {
            Menu.Info("  Cancelled.");
            return;
        }

        await api.RetireProductAsync(id);
        Menu.Info($"  Product #{id} was retired.");
    }
}
