namespace EcommerceApiConsole.Models;

// Client-side copies of the API DTOs. Deliberately NOT shared with the API project: the console app
// talks HTTP only, so the JSON contract stays the single source of truth.

public record Category(int Id, string Name, string Description, int ProductCount);

public record Product(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAtUtc);

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

public record SaleLine(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record Sale(int Id, string CustomerName, DateTime SoldAtUtc, decimal Total, List<SaleLine> Items);

public record CategoryRequest(string Name, string? Description);

public record ProductRequest(string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);

public record SaleLineRequest(int ProductId, int Quantity);

public record SaleRequest(string CustomerName, List<SaleLineRequest> Items);
