﻿using System;


namespace CryptonorClient
{
    static class  Mapper
    {
        public static string GetTagByType(Type type)
        {

            if (type == typeof(int))
                return "tags_int";
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return "tags_datetime";
            else if (type == typeof(float))
                return "tags_float";
            else if (type == typeof(decimal))
                return "tags_decimal";
            else if (type == typeof(double))
                return "tags_double";
            else if (type == typeof(string))
                return "tags_string";
            else if (type == typeof(long))
                return "tags_long";
            else if (type == typeof(bool))
                return "tags_bool";
            else if (type == typeof(Guid))
                return "tags_guid";


            throw new Exception("Type :" + type.ToString() + " not supported!");
        }
        
       
    }
}