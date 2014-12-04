﻿using Sqo.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if ASYNC
using System.Threading.Tasks;
#endif

namespace Cryptonor.Indexes
{
    class IndexQueryFinder
    {
        public static void FindOids(IBTree index, Cryptonor.Queries.Query query,List<int> oids)
        {
            IEnumerable<int> oidsFound = null;
            if (query.Value != null)
            {
                oidsFound = index.FindItem(query.Value);

            }
            else if (query.Start != null && query.End != null)
            {
                List<int> oidsStart = GetByStart(query.Descending, query.Start, index);
                List<int> oidsEnd = GetByEnd(query.Descending, query.End, index);
                oidsFound = oidsEnd.Intersect(oidsStart);

            }
            else if (query.Start != null && query.End == null)
            {
                oidsFound = GetByStart(query.Descending, query.Start, index);
            }
            else if (query.Start == null && query.End != null)
            {
                oidsFound = GetByEnd(query.Descending, query.End, index);
            }
            else if (query.In != null)
            {
                foreach (object objTarget in query.In)
                {
                    var oidsIn = index.FindItem(objTarget);
                    if (oidsIn != null)
                    {
                        oids.AddRange(oidsIn);

                    }
                }
            }
            if (oidsFound != null)
            {
                oids.AddRange(oidsFound);

            }
        }
#if ASYNC
        public static async Task FindOidsAsync(IBTree index, Cryptonor.Queries.Query query, List<int> oids)
        {
            IEnumerable<int> oidsFound = null;
            if (query.Value != null)
            {
                oidsFound = await index.FindItemAsync(query.Value);

            }
            else if (query.Start != null && query.End != null)
            {
                List<int> oidsStart = await GetByStartAsync(query.Descending, query.Start, index);
                List<int> oidsEnd = await GetByEndAsync(query.Descending, query.End, index);
                oidsFound = oidsEnd.Intersect(oidsStart);

            }
            else if (query.Start != null && query.End == null)
            {
                oidsFound = await GetByStartAsync(query.Descending, query.Start, index);
            }
            else if (query.Start == null && query.End != null)
            {
                oidsFound = await GetByEndAsync(query.Descending, query.End, index);
            }
            else if (query.In != null)
            {
                foreach (object objTarget in query.In)
                {
                    var oidsIn = await index.FindItemAsync(objTarget);
                    if (oidsIn != null)
                    {
                        oids.AddRange(oidsIn);

                    }
                }
            }
            if (oidsFound != null)
            {
                oids.AddRange(oidsFound);

            }
        }
#endif
        private static List<int> GetByStart(bool? desc, object start, IBTree index)
        {
            if (desc == true)
            {
                var oids = index.FindItemsLessThanOrEqual(start);
                oids.Reverse();
                return oids;
            }
            else
            {
                var oids = index.FindItemsBiggerThanOrEqual(start);
                oids.Reverse();
                return oids;
            }
        }
#if ASYNC
        private static async Task<List<int>> GetByStartAsync(bool? desc, object start, IBTree index)
        {
            if (desc == true)
            {
                var oids = await index.FindItemsLessThanOrEqualAsync(start);
                oids.Reverse();
                return oids;
            }
            else
            {
                var oids = await index.FindItemsBiggerThanOrEqualAsync(start);
                oids.Reverse();
                return oids;
            }
        }
#endif
        private static List<int> GetByEnd(bool? desc, object end, IBTree index)
        {
            if (desc == true)
                return index.FindItemsBiggerThanOrEqual(end);
            else
            {
                return index.FindItemsLessThanOrEqual(end);
            }
        }
#if ASYNC
        private static async Task<List<int>> GetByEndAsync(bool? desc, object end, IBTree index)
        {
            if (desc == true)
                return await index.FindItemsBiggerThanOrEqualAsync(end);
            else
            {
                return await index.FindItemsLessThanOrEqualAsync(end);
            }
        }
#endif
    }
}