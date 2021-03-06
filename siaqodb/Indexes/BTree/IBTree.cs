﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqo.Indexes
{
    interface IBTree
    {
        void AddItem(object new_key,int oid);
        IEnumerable<int> FindItem(object target_key);
        List<int> FindItemsLessThan(object target_key);
        List<int> FindItemsLessThanOrEqual(object target_key);
        List<int> FindItemsBiggerThan(object target_key);
        List<int> FindItemsBiggerThanOrEqual(object target_key);
        List<int> FindItemsStartsWith(object target_key,bool defaultComparer, StringComparison stringComparison);
        void DeleteItem(object target_key,int oid);
       
        string IndexName { get; }

        void Drop();
    }
   
}