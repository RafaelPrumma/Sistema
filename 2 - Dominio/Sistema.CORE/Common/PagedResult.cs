namespace Sistema.CORE.Common;

using Microsoft.EntityFrameworkCore;
using System.Threading;

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);

public static class IQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var count = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, count, page, pageSize);
    }
}
