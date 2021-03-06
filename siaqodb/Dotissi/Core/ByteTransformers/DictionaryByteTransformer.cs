﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dotissi.MetaObjects;
using System.Collections;
using Dotissi.Meta;

namespace Dotissi.Core
{
    class DictionaryByteTransformer:IByteTransformer
    {
        private ObjectSerializer serializer;
        private RawdataSerializer rawSerializer;
        private SqoTypeInfo ti;
        private FieldSqoInfo fi;
        int parentOID;
        public DictionaryByteTransformer(ObjectSerializer serializer, RawdataSerializer rawSerializer, SqoTypeInfo ti, FieldSqoInfo fi,int parentOID)
        {
            
            this.serializer = serializer;
            this.rawSerializer = rawSerializer;
            this.ti = ti;
            this.fi = fi;
            this.parentOID = parentOID;
        }
        #region IByteTransformer Members

        public byte[] GetBytes(object obj)
        {
            DictionaryInfo dInfo =null;

            if (parentOID > 0)
            {
                dInfo = serializer.GetDictInfoOfField(ti, parentOID, fi);
            }
            else
            {
                if (obj != null)
                {
                    IDictionary actualDict = (IDictionary)obj;
                    Type[] keyValueType = actualDict.GetType().GetGenericArguments();
                    if (keyValueType.Length != 2)
                    {
                        throw new Sqo.Exceptions.NotSupportedTypeException("Type:" + actualDict.GetType().ToString() + " is not supported");
                    }
                    int keyTypeId = Dotissi.Meta.MetaExtractor.GetAttributeType(keyValueType[0]);
                    int valueTypeId = Dotissi.Meta.MetaExtractor.GetAttributeType(keyValueType[1]);
                    dInfo = new DictionaryInfo();
                    dInfo.KeyTypeId = keyTypeId;
                    dInfo.ValueTypeId = valueTypeId;
                }
            }
            return rawSerializer.SerializeDictionary(obj, fi.Header.Length, ti.Header.version, dInfo, serializer);
        }

        public object GetObject(byte[] bytes)
        {
            return rawSerializer.DeserializeDictionary(fi.AttributeType, bytes, ti.Header.version,serializer,ti.Type,fi.Name);
        }

        #endregion

#if ASYNC_LMDB
        public async System.Threading.Tasks.Task<byte[]> GetBytesAsync(object obj)
        {
            DictionaryInfo dInfo = null;

            if (parentOID > 0)
            {
                dInfo = await serializer.GetDictInfoOfFieldAsync(ti, parentOID, fi).ConfigureAwait(false);
            }
            else
            {
                if (obj != null)
                {
                    IDictionary actualDict = (IDictionary)obj;
                    Type[] keyValueType = actualDict.GetType().GetGenericArguments();
                    if (keyValueType.Length != 2)
                    {
                        throw new Sqo.Exceptions.NotSupportedTypeException("Type:" + actualDict.GetType().ToString() + " is not supported");
                    }
                    int keyTypeId = Dotissi.Meta.MetaExtractor.GetAttributeType(keyValueType[0]);
                    int valueTypeId = Dotissi.Meta.MetaExtractor.GetAttributeType(keyValueType[1]);
                    dInfo = new DictionaryInfo();
                    dInfo.KeyTypeId = keyTypeId;
                    dInfo.ValueTypeId = valueTypeId;
                }
            }
            return await rawSerializer.SerializeDictionaryAsync(obj, fi.Header.Length, ti.Header.version, dInfo, serializer).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<object> GetObjectAsync(byte[] bytes)
        {
            return await rawSerializer.DeserializeDictionaryAsync(fi.AttributeType, bytes, ti.Header.version, serializer, ti.Type, fi.Name).ConfigureAwait(false);
        }
#endif
    }
}
