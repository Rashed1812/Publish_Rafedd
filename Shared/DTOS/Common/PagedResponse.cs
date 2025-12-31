namespace Shared.DTOS.Common
{
    public class PagedResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "تمت العملية بنجاح";
        public List<T> Data { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = null!;

        public static PagedResponse<T> Create(
            List<T> data,
            int totalCount,
            int page,
            int pageSize,
            string message = "تمت العملية بنجاح")
        {
            return new PagedResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Pagination = new PaginationMetadata
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };
        }
    }

    public class PaginationMetadata
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }
}
