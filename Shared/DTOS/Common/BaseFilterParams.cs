namespace Shared.DTOS.Common
{
    public abstract class BaseFilterParams
    {
        // Pagination - optional with defaults
        public int? Page { get; set; }
        public int? PageSize { get; set; }

        // Sorting - optional
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }

        // Computed properties
        public int GetPage() => Page ?? 1;

        public int GetPageSize()
        {
            const int maxPageSize = 100;
            const int defaultPageSize = 10;

            if (!PageSize.HasValue) return defaultPageSize;
            return PageSize.Value > maxPageSize ? maxPageSize : PageSize.Value;
        }

        public bool IsDescending => SortOrder?.ToLower() == "desc";

        public int Skip => (GetPage() - 1) * GetPageSize();
    }
}
