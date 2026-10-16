using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApi.Models;

public class Sale
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime SoldAtUtc { get; set; }

    /// <summary>Soft delete = the sale was voided.</summary>
    public bool IsDeleted { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    [NotMapped]
    public decimal Total => SaleItems.Sum(i => i.Quantity * i.UnitPrice);
}
