﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqo.MetaObjects
{
    class ArrayInfo
    {
        public int NrElements;
        public int ElementTypeId;//used only if is jagged array
        public byte[] rawArray;
    }
}
