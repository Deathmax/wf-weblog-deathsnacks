using System.Collections.Generic;
using System.Linq;

namespace Warframe_WebLog.Helpers
{
    public static class PagingUtils
    {
        public static IEnumerable<string> Page(this IEnumerable<string> en, int pageSize, int page)
        {
            return en.Skip(page*pageSize).Take(pageSize);
        }

        public static IQueryable<string> Page(this IQueryable<string> en, int pageSize, int page)
        {
            return en.Skip(page*pageSize).Take(pageSize);
        }
    }
}