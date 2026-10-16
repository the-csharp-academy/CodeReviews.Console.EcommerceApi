namespace EcommerceApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Set at creation and never updated — changing it would rewrite sales history.</summary>
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
