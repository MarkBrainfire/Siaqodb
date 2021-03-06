﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiaqodbSyncProvider.Utilities
{
    class JSerializer
    {
        public static byte[] Serialize(object obj)
        {
            string jsonStr = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(jsonStr);
        }
        public static object Deserialize(Type type, byte[] objectBytes)
        {
#if WinRT
            string jsonStr = Encoding.UTF8.GetString(objectBytes,0,objectBytes.Length);
#else
             string jsonStr = Encoding.UTF8.GetString(objectBytes);
#endif
            return JsonConvert.DeserializeObject(jsonStr.TrimEnd('\0'), type);
        }
    }
}
