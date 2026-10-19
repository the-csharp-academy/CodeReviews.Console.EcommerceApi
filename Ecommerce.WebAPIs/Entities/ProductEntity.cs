namespace Ecommerce.WebAPIs.Entities
{
    public class ProductEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public CategoryEntity Category { get; set; }
        public List<SaleEntity> Sales { get; set; } = new();

        private int _quantity;
        public int Quantity
        {
            get
            {
                return _quantity;
            }
            set
            {
                if (_quantity < 0)
                    throw new ArgumentException("Quantity can not be less then 0");
                if (_quantity < Sales.Count)
                    throw new ArgumentException("Sales are more then quantity");
                _quantity = value;
            }
        }
        public bool IsDeleted { get; set; }
    }
}