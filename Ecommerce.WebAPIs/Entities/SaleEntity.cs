namespace Ecommerce.WebAPIs.Entities
{
    public class SaleEntity
    {
        public int Id { get; set; }
        public List<ProductEntity> Products { get; set; } = new();
        public DateTime SaleTime { get; set; }
        public bool IsDeleted {  get; set; }
    }
}
