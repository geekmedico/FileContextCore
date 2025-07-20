using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileContextCore.Extensions
{
    public static class QueryableExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Task.Run(() => source.ToList(), cancellationToken);
        }
    }
}
