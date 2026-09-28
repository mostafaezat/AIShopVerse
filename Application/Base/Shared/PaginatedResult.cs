namespace Application.Base.Shared
{
    public interface IPaginatedRequest<T> where T : class
    {
        int Page { get; set; }
        int PageSize { get; set; }
    }

    public class PaginatedRequest<T> : IPaginatedRequest<T> where T : class
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;
    }

    public class PaginatedResult<T> where T : class
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public static PaginatedResult<T> Create(List<T> items, int totalItems, int page, int pageSize)
        {
            return new PaginatedResult<T>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
