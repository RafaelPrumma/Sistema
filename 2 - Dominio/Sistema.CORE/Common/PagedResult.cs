namespace Sistema.CORE.Common;

using Microsoft.EntityFrameworkCore;

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);

public static class IQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var count = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T>(items, count, page, pageSize);
    }
}
