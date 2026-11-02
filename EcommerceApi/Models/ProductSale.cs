namespace EcommerceApi.Models
{
    public class ProductSale
    {
        public int ProductId { get; set; }

        public int SaleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }
        public Sale? Sale { get; set; }
    }
}
