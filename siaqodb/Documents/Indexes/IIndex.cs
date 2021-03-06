﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Sqo.Documents.Indexes
{
    interface IIndex : IDisposable
    {
        List<string> FindItem(object key);
        List<string> FindAllExcept(object key);
        List<string> FindItemsBetween(object start, object end);
        List<string> FindItemsBetweenExceptStart(object start, object end);
        List<string> FindItemsBetweenExceptEnd(object start, object end);
        List<string> FindItemsBetweenExceptStartEnd(object start, object end);
        List<string> FindItemsBiggerThan(object start);
        List<string> FindItemsBiggerThanOrEqual(object start);
        List<string> FindItemsLessThan(object start);
        List<string> FindItemsLessThanOrEqual(object start);
        List<string> FindItemsStartsWith(object target_key);
        List<string> FindItemsContains(object target_key);
        List<string> FindItemsEndsWith(object target_key);



    }
}
