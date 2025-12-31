using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DAL.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Apply pagination to a queryable
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            var skip = (page - 1) * pageSize;
            return query.Skip(skip).Take(pageSize);
        }

        /// <summary>
        /// Apply dynamic sorting with optional default fallback
        /// </summary>
        public static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> query,
            string? sortBy,
            bool isDescending,
            Dictionary<string, Expression<Func<T, object>>> sortExpressions,
            Expression<Func<T, object>>? defaultSort = null)
        {
            if (!string.IsNullOrEmpty(sortBy) && sortExpressions.ContainsKey(sortBy.ToLower()))
            {
                var expression = sortExpressions[sortBy.ToLower()];
                return isDescending
                    ? query.OrderByDescending(expression)
                    : query.OrderBy(expression);
            }

            // Apply default sort if provided and no valid sortBy
            if (defaultSort != null)
            {
                return isDescending
                    ? query.OrderByDescending(defaultSort)
                    : query.OrderBy(defaultSort);
            }

            return query;
        }

        /// <summary>
        /// Apply date range filter to a queryable
        /// </summary>
        public static IQueryable<T> ApplyDateRange<T>(
            this IQueryable<T> query,
            DateTime? startDate,
            DateTime? endDate,
            Expression<Func<T, DateTime>> dateProperty)
        {
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                var parameter = dateProperty.Parameters[0];
                var predicate = Expression.Lambda<Func<T, bool>>(
                    Expression.GreaterThanOrEqual(dateProperty.Body, Expression.Constant(start)),
                    parameter);
                query = query.Where(predicate);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                var parameter = dateProperty.Parameters[0];
                var predicate = Expression.Lambda<Func<T, bool>>(
                    Expression.LessThan(dateProperty.Body, Expression.Constant(end)),
                    parameter);
                query = query.Where(predicate);
            }

            return query;
        }

        /// <summary>
        /// Get paged result with total count in one call
        /// </summary>
        public static async Task<(List<T> Items, int TotalCount)> ToPagedListAsync<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            var totalCount = await query.CountAsync();
            var items = await query
                .ApplyPagination(page, pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
