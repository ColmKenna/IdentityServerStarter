using Microsoft.EntityFrameworkCore;

namespace EfCoreExtensions;

public static class QueryableExtensions
{
    public static async Task<IReadOnlyList<TSource>> ToReadOnlyListAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TSource>();
        await foreach (var element in source.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(element);
        }

        return list.AsReadOnly();
    }
}