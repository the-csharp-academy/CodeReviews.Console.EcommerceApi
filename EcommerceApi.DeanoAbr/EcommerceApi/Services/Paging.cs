namespace EcommerceApi.Services;

public static class Paging
{
    public const int MaxPageSize = 50;

    public static (int Page, int PageSize) Validate(int page, int pageSize)
    {
        if (page < 1) throw new ValidationException("page must be at least 1.");
        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ValidationException($"pageSize must be between 1 and {MaxPageSize}.");
        }

        return (page, pageSize);
    }
}
