namespace Ecommerce.WebAPIs
{
    public class PaginationParams
    {
        private const int MaxPageSize = 50;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => field;
            set
            {
                if (value > MaxPageSize)
                    field = MaxPageSize;
                else field = value;
            }
        }

        public string? Category {  get; set; }
        public string? SearchTerm { get; set; }
        public bool? SortByPriceAsc { get; set; }
        public bool? SortByPriceDesc { get; set; }
        public bool? SortByNameAsc { get; set; }
        public bool? SortByNameDesc { get; set; }
    }
}
