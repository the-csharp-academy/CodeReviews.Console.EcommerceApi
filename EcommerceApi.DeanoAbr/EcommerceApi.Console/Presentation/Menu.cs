using EcommerceApiConsole.Models;

namespace EcommerceApiConsole.Presentation;

/// <summary>All console reading and writing lives here, so the menu code in Program.cs stays readable.</summary>
public static class Menu
{
    public const int PageSize = 10;

    /// <summary>True once stdin returns EOF, so the loop can exit instead of spinning on empty input.</summary>
    public static bool InputClosed { get; private set; }

    public static void Banner()
    {
        Console.WriteLine();
        Console.WriteLine("=== Bean & Barrel ecommerce console ===");
        Console.WriteLine("Talks to the Ecommerce API at http://localhost:5276");
    }

    public static void Options()
    {
        Console.WriteLine();
        Console.WriteLine("  1) Browse products");
        Console.WriteLine("  2) Search / filter products");
        Console.WriteLine("  3) Product details");
        Console.WriteLine("  4) List categories");
        Console.WriteLine("  5) Create a category");
        Console.WriteLine("  6) Create a product");
        Console.WriteLine("  7) Record a sale");
        Console.WriteLine("  8) Browse sales");
        Console.WriteLine("  9) Sale details");
        Console.WriteLine(" 10) Void a sale");
        Console.WriteLine(" 11) Retire a product");
        Console.WriteLine("  0) Exit");
    }

    public static string? Read(string prompt)
    {
        Console.Write(prompt);
        var line = Console.ReadLine();
        if (line is null) InputClosed = true;
        return line?.Trim();
    }

    public static string ReadRequired(string prompt)
    {
        while (true)
        {
            var value = Read(prompt);
            if (InputClosed) return string.Empty;
            if (!string.IsNullOrWhiteSpace(value)) return value;
            Console.WriteLine("  A value is required.");
        }
    }

    public static int ReadInt(string prompt, int minimum = 1)
    {
        while (true)
        {
            var value = Read(prompt);
            if (InputClosed) return minimum;
            if (int.TryParse(value, out var number) && number >= minimum) return number;
            Console.WriteLine($"  Enter a whole number of at least {minimum}.");
        }
    }

    public static int? ReadOptionalInt(string prompt)
    {
        var value = Read(prompt);
        if (InputClosed) return null;
        return int.TryParse(value, out var number) ? number : null;
    }

    public static decimal? ReadOptionalDecimal(string prompt)
    {
        var value = Read(prompt);
        if (InputClosed) return null;
        return decimal.TryParse(value, out var number) ? number : null;
    }

    public static decimal ReadDecimal(string prompt, decimal minimum = 0.01m)
    {
        while (true)
        {
            var value = Read(prompt);
            if (InputClosed) return minimum;
            if (decimal.TryParse(value, out var number) && number >= minimum) return number;
            Console.WriteLine($"  Enter a number of at least {minimum:0.00}.");
        }
    }

    public static bool Confirm(string prompt)
    {
        var value = Read($"{prompt} [y/N]: ");
        return !InputClosed && value is not null && value.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    public static void Pause()
    {
        if (InputClosed) return;
        Read("Press Enter to continue...");
    }

    public static void Info(string message) => Console.WriteLine(message);

    public static void Error(string message)
    {
        Console.WriteLine();
        Console.WriteLine($"  ! {message}");
    }

    public static void PrintCategories(IReadOnlyList<Category> categories)
    {
        Console.WriteLine();
        Console.WriteLine($"  {"id",-4} {"name",-24} {"products",-9} description");
        Console.WriteLine($"  {new string('-', 4)} {new string('-', 24)} {new string('-', 9)} {new string('-', 30)}");
        foreach (var category in categories)
        {
            Console.WriteLine($"  {category.Id,-4} {Trim(category.Name, 24),-24} {category.ProductCount,-9} {Trim(category.Description, 40)}");
        }
    }

    public static void PrintProducts(IReadOnlyList<Product> products)
    {
        Console.WriteLine();
        Console.WriteLine($"  {"id",-4} {"name",-30} {"price",10} {"stock",6} {"category",-20}");
        Console.WriteLine($"  {new string('-', 4)} {new string('-', 30)} {new string('-', 10)} {new string('-', 6)} {new string('-', 20)}");
        foreach (var product in products)
        {
            Console.WriteLine($"  {product.Id,-4} {Trim(product.Name, 30),-30} {product.Price,10:0.00} {product.StockQuantity,6} {Trim(product.CategoryName, 20),-20}");
        }
    }

    public static void PrintProduct(Product product)
    {
        Console.WriteLine();
        Console.WriteLine($"  #{product.Id} {product.Name}");
        Console.WriteLine($"    {product.Description}");
        Console.WriteLine($"    price {product.Price:0.00} | stock {product.StockQuantity} | category {product.CategoryName} (#{product.CategoryId})");
        Console.WriteLine($"    added {product.CreatedAtUtc:yyyy-MM-dd}");
    }

    public static void PrintSales(IReadOnlyList<Sale> sales)
    {
        Console.WriteLine();
        Console.WriteLine($"  {"id",-4} {"sold",-20} {"customer",-20} {"total",10} {"lines",6}");
        Console.WriteLine($"  {new string('-', 4)} {new string('-', 20)} {new string('-', 20)} {new string('-', 10)} {new string('-', 6)}");
        foreach (var sale in sales)
        {
            Console.WriteLine($"  {sale.Id,-4} {sale.SoldAtUtc.ToString("yyyy-MM-dd HH:mm"),-20} {Trim(sale.CustomerName, 20),-20} {sale.Total,10:0.00} {sale.Items.Count,6}");
        }
    }

    public static void PrintSale(Sale sale)
    {
        Console.WriteLine();
        Console.WriteLine($"  Sale #{sale.Id} for {sale.CustomerName} on {sale.SoldAtUtc:yyyy-MM-dd HH:mm} (UTC)");
        Console.WriteLine($"  {"product",-30} {"qty",4} {"unit",10} {"line",10}");
        foreach (var line in sale.Items)
        {
            Console.WriteLine($"  {Trim(line.ProductName, 30),-30} {line.Quantity,4} {line.UnitPrice,10:0.00} {line.LineTotal,10:0.00}");
        }
        Console.WriteLine($"  {"",-30} {"",4} {"total",10} {sale.Total,10:0.00}");
    }

    public static void PrintPagedFooter(string label, int page, int pageSize, int totalCount, int totalPages) =>
        Console.WriteLine($"  -- {label}: page {page} of {Math.Max(totalPages, 1)}, {totalCount} in total, {pageSize} per page --");

    private static string Trim(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";
}
