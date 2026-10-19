namespace Ecommerce.WebAPIs.DTOs
{
    public class SaleResponseDto
    {
        public int Id { get; set; }
        public List<ProductResponseDto> Products { get; set; }
        public DateTime SaleTime { get; set; }
    }
}
