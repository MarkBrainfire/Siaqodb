﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqo.Attributes
{
     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TextAttribute:System.Attribute
    {
         public TextAttribute()
         {

         }
    }
}
