﻿using System;

namespace LightningDB.Converters
{
    /// <summary>
    /// Thrown when a converter wasn't fount
    /// </summary>
    internal class ConverterNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new instance of ConverterNotFoundException
        /// </summary>
        /// <param name="type">Type for which the converter wasn't found</param>
        public ConverterNotFoundException(Type type) 
            : base(string.Format("Unable to find converter for {0}", type.FullName))
        {
        }
    }
}