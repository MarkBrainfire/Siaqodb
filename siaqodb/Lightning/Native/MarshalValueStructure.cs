﻿using System;
using System.Runtime.InteropServices;

namespace LightningDB.Native
{
    internal class MarshalValueStructure : IDisposable
    {
        private bool _shouldDispose;
        private byte[] _value;

        private ValueStructure _structure;

        public MarshalValueStructure(byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            _shouldDispose = false;
            _value = value;

            this.ValueInitialized = false;
        }

        public bool ValueInitialized { get; private set; }

        private IntPtr GetSize(byte[] array)
        {
            return new IntPtr(array.Length);
        }

        //Lazy initialization prevents possible leak.
        //If initialization was in ctor, Dispose could never be called
        //due to possible exception thrown by Marshal.Copy
        public ValueStructure ValueStructure
        {
            get
            {
                if (!this.ValueInitialized)
                {
                    var size = this.GetSize(_value);
                    
                    _structure = new ValueStructure
                    {
#if SILVERLIGHT
                        data = Marshal.AllocHGlobal((int)size)
#else
                        data = Marshal.AllocHGlobal(size)
#endif
,
                        size = size
                    };

                    _shouldDispose = true;

                    try
                    {
                        Marshal.Copy(_value, 0, _structure.data, _value.Length);
                    }
                    catch
                    {
                        this.Dispose();
                    }

                    this.ValueInitialized = true;
                }

                return _structure;
            }
        }

        protected virtual void Dispose(bool shouldDispose)
        {
            if (!shouldDispose)
                return;

            try
            {
                Marshal.FreeHGlobal(_structure.data);
            }
            catch { }
        }

        public void Dispose()
        {
            this.Dispose(_shouldDispose);
            _shouldDispose = false;
        }
    }
}
