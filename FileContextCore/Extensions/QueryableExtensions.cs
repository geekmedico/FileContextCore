using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FileContextCore.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var list = new List<T>();
            await foreach (var element in source.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                list.Add(element);
            }

            return list;
        }
    }
}
