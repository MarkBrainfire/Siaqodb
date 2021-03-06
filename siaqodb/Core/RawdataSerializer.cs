﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Sqo.Meta;
using System.IO;
using Sqo.MetaObjects;
using Sqo.Utilities;
using LightningDB;


namespace Sqo.Core
{
    class RawdataSerializer
    {
       

        StorageEngine storageEngine;
        bool useElevatedTrust;    
        static readonly object _syncRoot = new object();
      
        public RawdataSerializer(StorageEngine storageEngine,bool useElevatedTrust)
        {
            this.storageEngine = storageEngine;
            this.useElevatedTrust = useElevatedTrust;
            
        }
     
        public byte[] SerializeArray(object obj, Type objectType, int length, int realLength, int dbVersion, string dbName,string fieldName, ObjectSerializer objSerializer, bool elementIsText,int parentOID, LightningDB.LightningTransaction transaction)
        {

            byte[] b = new byte[length];
            if (obj == null)
            {
                b[0] = 1;//is null
                return b;
            }
            else
            {
                b[0] = 0;
            }


            ArrayInfo arrayInfo = this.SerializeArray(obj, objectType, objSerializer, dbVersion, elementIsText,transaction);
            //notused anymore
            //byte[] rawOIDBytes = ByteConverter.SerializeValueType(rawOID, typeof(int), dbVersion);
            //Array.Copy(rawOIDBytes, 0, b, 1, rawOIDBytes.Length);
            byte[] nrElementsBytes = ByteConverter.SerializeValueType(arrayInfo.NrElements, typeof(int), dbVersion);
          
            Array.Copy(nrElementsBytes, 0, b, 5, nrElementsBytes.Length);

            string rawKey = parentOID + fieldName;
            var db = transaction.OpenDatabase(dbName, DatabaseOpenFlags.Create);

            byte[] key = ByteConverter.StringToByteArray(rawKey);
            transaction.Put(db, key, arrayInfo.rawArray);

            return b;


        }


        private ArrayInfo SerializeArray(object obj, Type objectType, ObjectSerializer objSerializer,int dbVersion,bool elementIsText,LightningDB.LightningTransaction transaction)
        {
            
            ArrayInfo arrInfo = new ArrayInfo();

            if (objectType == typeof(string))//text
            {
                arrInfo.NrElements = Encoding.UTF8.GetByteCount((string)obj);

                //optimize serialization and gets directly bytes of string not serialize every byte
                arrInfo.rawArray = ByteConverter.SerializeValueType(obj, objectType, MetaHelper.PaddingSize(arrInfo.NrElements), arrInfo.NrElements, dbVersion);
                
            }
            else if (objectType == typeof(byte[]))
            {
                arrInfo.NrElements = ((byte[])obj).Length;
                //optimize serialization and gets directly bytes of string not serialize every byte
                arrInfo.rawArray = ByteConverter.SerializeValueType(obj, objectType, MetaHelper.PaddingSize(arrInfo.NrElements), arrInfo.NrElements, dbVersion);
                
            }
            else
            {
                Type elementType=null;
                if(objectType.IsGenericType())
                {
                    elementType = objectType.GetProperty("Item").PropertyType;
                }
                else
                {
                    elementType=objectType.GetElementType();
                }
                
                int elementTypeId = MetaExtractor.GetAttributeType(elementType);
                if (typeof(IList).IsAssignableFrom(elementType))
                {
                    elementTypeId = MetaExtractor.jaggedArrayID;
                }
                else if (elementIsText && elementType==typeof(string))
                {
                    elementTypeId = MetaExtractor.textID;
                }
                int elementSize=MetaExtractor.GetSizeOfField(elementTypeId);

                arrInfo.NrElements = ((IList)obj).Count;
                arrInfo.ElementTypeId = elementTypeId;
                int rawLength = arrInfo.NrElements * elementSize;
                byte[] rawArray = new byte[rawLength];
                //build array for elements
                int currentIndex = 0;
                foreach (object elem in ((IList)obj))
                {
                    
                    byte[] elemArray = null;
                    if (elementTypeId == MetaExtractor.complexID)
                    {
                        elemArray = objSerializer.GetComplexObjectBytes(elem,transaction);
                    }
                    else if (elementTypeId == MetaExtractor.jaggedArrayID)
                    {
                        elemArray = new byte[elementSize];
                        if (elem == null)
                        {
                            elemArray[0] = 1;
                        }
                        else
                        {
                            ArrayInfo arrElemInfo = SerializeArray(elem, elementType, objSerializer, dbVersion, elementIsText,transaction);
                            byte[] jaggedArray = arrElemInfo.rawArray;
                            
                            int jaggedArrayLength=jaggedArray.Length + elementSize;

                            elemArray = new byte[jaggedArrayLength];
                            
                            #region jaggedArrayMetaData
                            byte[] nrElementsBytes = ByteConverter.SerializeValueType(arrElemInfo.NrElements, typeof(int), dbVersion);
                            byte[] elemTypeIdBytes = ByteConverter.SerializeValueType(arrElemInfo.ElementTypeId, typeof(int), dbVersion);
                            byte[] elemArrayLengthBytes = ByteConverter.SerializeValueType(jaggedArrayLength, typeof(int), dbVersion);
                            int index = 1;
                            Array.Copy(nrElementsBytes, 0, elemArray, index, nrElementsBytes.Length);
                            index += nrElementsBytes.Length;
                            Array.Copy(elemTypeIdBytes, 0, elemArray, index, elemTypeIdBytes.Length);
                            index += elemTypeIdBytes.Length;
                            Array.Copy(elemArrayLengthBytes, 0, elemArray, index, elemArrayLengthBytes.Length);

                            #endregion


                            Array.Copy(jaggedArray, 0, elemArray, elementSize, jaggedArray.Length);

                        }
                        if (rawArray.Length - currentIndex < elemArray.Length)
                        {
                            Array.Resize<byte>(ref rawArray, elemArray.Length + currentIndex);

                        }
                    }
                    else if (elementTypeId == MetaExtractor.textID)
                    {
                        elemArray = new byte[elementSize];
                        if (elem == null)
                        {
                            elemArray[0] = 1;
                        }
                        else
                        {
                            int nrChars = Encoding.UTF8.GetByteCount((string)elem);

                            byte[] jaggedArray = ByteConverter.SerializeValueType(elem, typeof(string), MetaHelper.PaddingSize(nrChars), nrChars, dbVersion);
          
                             

                            int jaggedArrayLength = jaggedArray.Length + elementSize;

                            elemArray = new byte[jaggedArrayLength];

                            #region jaggedArrayMetaData
                            byte[] nrElementsBytes = ByteConverter.SerializeValueType(nrChars, typeof(int), dbVersion);
                            byte[] elemTypeIdBytes = ByteConverter.SerializeValueType(elementTypeId, typeof(int), dbVersion);
                            byte[] elemArrayLengthBytes = ByteConverter.SerializeValueType(jaggedArrayLength, typeof(int), dbVersion);
                            int index = 1;
                            Array.Copy(nrElementsBytes, 0, elemArray, index, nrElementsBytes.Length);
                            index += nrElementsBytes.Length;
                            Array.Copy(elemTypeIdBytes, 0, elemArray, index, elemTypeIdBytes.Length);
                            index += elemTypeIdBytes.Length;
                            Array.Copy(elemArrayLengthBytes, 0, elemArray, index, elemArrayLengthBytes.Length);

                            #endregion


                            Array.Copy(jaggedArray, 0, elemArray, elementSize, jaggedArray.Length);

                        }
                        if (rawArray.Length - currentIndex < elemArray.Length)
                        {
                            Array.Resize<byte>(ref rawArray, elemArray.Length + currentIndex);

                        }
                    }
                    else if (elem is IDictionary)
                    {
                        throw new Sqo.Exceptions.NotSupportedTypeException("IDictionary it is not supported type as IList element type.");
                    }
                    else
                    {
                        object elemObj = elem;
                        if (elem == null)
                        {
                            if (elementType == typeof(string))
                            {
                                elemObj = string.Empty;
                            }
                        }
                        elemArray = ByteConverter.SerializeValueType(elemObj, elemObj.GetType(), elementSize, MetaExtractor.GetAbsoluteSizeOfField(elementTypeId), dbVersion);
                    }
                    
                    Array.Copy(elemArray, 0, rawArray, currentIndex, elemArray.Length);
                    
                    currentIndex += elemArray.Length;
                }
                arrInfo.rawArray = rawArray;
            }
            return arrInfo;

        }

      
        public byte[] SerializeDictionary(object obj, int length, int dbVersion, DictionaryInfo dictInfo,ObjectSerializer objSerializer,string dbName,string fieldName,int parentOID,LightningDB.LightningTransaction transaction)
        {
            byte[] b = new byte[length];
            if (obj == null)
            {
                b[0] = 1;//is null
                return b;
            }
            else
            {
                b[0] = 0;
            }

            int nrElements = 0;
            int rawLength = 0;
            byte[] rawArray = null;

            nrElements = ((IDictionary)obj).Keys.Count;

            rawLength = nrElements * (MetaExtractor.GetSizeOfField(dictInfo.KeyTypeId) + MetaExtractor.GetSizeOfField(dictInfo.ValueTypeId));
            rawArray = new byte[rawLength];
            //build array for elements
            int currentIndex = 0;
            IDictionary dictionary= (IDictionary)obj;
            foreach (object elem in dictionary.Keys)
            {
                byte[] keyArray = null;
                byte[] valueArray = null;
               
                #region key
                if (dictInfo.KeyTypeId == MetaExtractor.complexID )
                {
                    keyArray = objSerializer.GetComplexObjectBytes(elem,transaction);
                }
                else if (elem is IList)
                {
                    throw new Sqo.Exceptions.NotSupportedTypeException("Array/IList as Type of Key of a Dictionary it is not supported");
                }
                else
                {
                    keyArray = ByteConverter.SerializeValueType(elem, elem.GetType(), MetaExtractor.GetSizeOfField(dictInfo.KeyTypeId), MetaExtractor.GetAbsoluteSizeOfField(dictInfo.KeyTypeId), dbVersion);
                }
                Array.Copy(keyArray, 0, rawArray, currentIndex, keyArray.Length);
                currentIndex += keyArray.Length;
                #endregion

                #region value
                if (dictInfo.ValueTypeId == MetaExtractor.complexID )
                {
                    valueArray = objSerializer.GetComplexObjectBytes(dictionary[elem],transaction);
                }
                else if (dictionary[elem] is IList)
                {
                    throw new Sqo.Exceptions.NotSupportedTypeException("Array/IList as Type of Value of a Dictionary it is not supported");
                }
                else
                {
                    valueArray = ByteConverter.SerializeValueType(dictionary[elem], dictionary[elem].GetType(), MetaExtractor.GetSizeOfField(dictInfo.ValueTypeId), MetaExtractor.GetAbsoluteSizeOfField(dictInfo.ValueTypeId), dbVersion);
                }
                Array.Copy(valueArray, 0, rawArray, currentIndex, valueArray.Length);
                currentIndex += valueArray.Length;
                #endregion

            }

            //byte[] rawOIDBytes = ByteConverter.SerializeValueType(rawOID, typeof(int), dbVersion);
            byte[] nrElementsBytes = ByteConverter.SerializeValueType(nrElements, typeof(int), dbVersion);
            byte[] keyTypeIdBytes = ByteConverter.SerializeValueType(dictInfo.KeyTypeId, typeof(int), dbVersion);
            byte[] valueTypeIdBytes = ByteConverter.SerializeValueType(dictInfo.ValueTypeId, typeof(int), dbVersion);

            int index = 5;
            
            Array.Copy(nrElementsBytes, 0, b, index, nrElementsBytes.Length);
            index += nrElementsBytes.Length;
            Array.Copy(keyTypeIdBytes, 0, b, index, keyTypeIdBytes.Length);
            index += keyTypeIdBytes.Length;
            Array.Copy(valueTypeIdBytes, 0, b, index, valueTypeIdBytes.Length);

            string rawKey = parentOID + fieldName;
            var db = transaction.OpenDatabase(dbName, DatabaseOpenFlags.Create);

            byte[] key = ByteConverter.StringToByteArray(rawKey);
            transaction.Put(db, key, rawArray);

            return b;
        }


        public object DeserializeDictionary(Type objectType, byte[] bytes, int dbVersion, ObjectSerializer objSerializer, Type parentType,string dbName, string fieldName,int parentOID, LightningDB.LightningTransaction transaction)
        {
            if (bytes[0] == 1) //is null
            {
                return null;
            }
            byte[] oidBytes = new byte[4];
            Array.Copy(bytes, 1, oidBytes, 0, 4);
            int rawInfoOID = (int)ByteConverter.DeserializeValueType(typeof(int), oidBytes, dbVersion);

            byte[] nrElemeBytes = new byte[4];
            Array.Copy(bytes, oidBytes.Length+1, nrElemeBytes, 0, 4);
            int nrElem = (int)ByteConverter.DeserializeValueType(typeof(int), nrElemeBytes, dbVersion);

            byte[] keyTypeIdBytes = new byte[4];
            Array.Copy(bytes, oidBytes.Length + nrElemeBytes.Length + 1, keyTypeIdBytes, 0, 4);
            int keyTypeId = (int)ByteConverter.DeserializeValueType(typeof(int), keyTypeIdBytes, dbVersion);

            byte[] valueTypeIdBytes = new byte[4];
            Array.Copy(bytes, oidBytes.Length + nrElemeBytes.Length+keyTypeIdBytes.Length + 1, valueTypeIdBytes, 0, 4);
            int valueTypeId = (int)ByteConverter.DeserializeValueType(typeof(int), valueTypeIdBytes, dbVersion);


            string rawKey = parentOID + fieldName;
            var db = transaction.OpenDatabase(dbName, DatabaseOpenFlags.Create);
            byte[] dbkey = ByteConverter.StringToByteArray(rawKey);
            byte[] arrayData = transaction.Get(db, dbkey);

            if (arrayData == null)
                return null;

            object objToReturn = Activator.CreateInstance(objectType);
            IDictionary actualDict = (IDictionary)objToReturn;
            int currentIndex = 0;

            Type[] keyValueType = actualDict.GetType().GetGenericArguments();
            if (keyValueType.Length != 2)
            {
                throw new Sqo.Exceptions.NotSupportedTypeException("Type:" + actualDict.GetType().ToString() + " is not supported");
            }
            Type keyType = keyValueType[0];
            Type valueType = keyValueType[1];


            int keyLen = MetaExtractor.GetSizeOfField(keyTypeId);
            int valueLen= MetaExtractor.GetSizeOfField(valueTypeId);
            byte[] keyBytes = new byte[keyLen];
            byte[] valueBytes = new byte[valueLen];
            for (int i = 0; i < nrElem; i++)
            {
                object key=null;
                Array.Copy(arrayData, currentIndex, keyBytes, 0, keyLen);
                if (keyTypeId == MetaExtractor.complexID)
                {
                    key = objSerializer.ReadComplexObject(keyBytes, parentType, fieldName, transaction);
                }
                else
                {
                    key = ByteConverter.DeserializeValueType(keyType, keyBytes, true, dbVersion);
                }
                currentIndex += keyLen;

                object val = null;
                Array.Copy(arrayData, currentIndex, valueBytes, 0, valueLen);
                if (valueTypeId == MetaExtractor.complexID)
                {
                    val = objSerializer.ReadComplexObject(valueBytes, parentType, fieldName, transaction);
                }
                else
                {
                    val = ByteConverter.DeserializeValueType(valueType, valueBytes, true, dbVersion);
                }
                currentIndex += valueLen;

                actualDict.Add(key, val);
                

            }
            
            return objToReturn;

        }

        public object DeserializeArray(Type objectType, byte[] bytes, bool checkEncrypted, int dbVersion, bool isText,bool elementIsText, ObjectSerializer objSerializer, Type parentType,string dbName, string fieldName,int parentOID,LightningDB.LightningTransaction transaction)
        {

            bool isArray = objectType.IsArray;

            if (bytes[0] == 1) //is null
            {
                return null;
            }
            //notused
            /*byte[] oidBytes = new byte[4];
            Array.Copy(bytes, 1, oidBytes, 0, 4);
            int rawInfoOID = (int)ByteConverter.DeserializeValueType(typeof(int), oidBytes, dbVersion);
            */
            byte[] nrElemeBytes = new byte[4];
            Array.Copy(bytes, MetaExtractor.ExtraSizeForArray - 4, nrElemeBytes, 0, 4);
            int nrElem = (int)ByteConverter.DeserializeValueType(typeof(int), nrElemeBytes, dbVersion);

            string rawKey = parentOID + fieldName;
            var db = transaction.OpenDatabase(dbName, DatabaseOpenFlags.Create);
            byte[] key = ByteConverter.StringToByteArray(rawKey);
            byte[] arrayData = transaction.Get(db, key);

            if (arrayData == null)
                return null;
           

            return DeserializeArrayInternal(objectType, arrayData, checkEncrypted, dbVersion, isText,elementIsText,nrElem,objSerializer,parentType,fieldName,transaction);

        }

        private object DeserializeArrayInternal(Type objectType, byte[] arrayData, bool checkEncrypted, int dbVersion, bool isText, bool elementIsText, int nrElem, ObjectSerializer objSerializer, Type parentType, string fieldName, LightningDB.LightningTransaction transaction)
        {
            bool isArray = objectType.IsArray;
            Type elementType = objectType.GetElementType();
            if (!isArray && !isText)
            {
                elementType = objectType.GetProperty("Item").PropertyType;
            }
            else if (isText)
            {
                elementType = typeof(string);
            }
            int elementTypeId = MetaExtractor.GetAttributeType(elementType);
            if (typeof(IList).IsAssignableFrom(elementType))
            {
                elementTypeId = MetaExtractor.jaggedArrayID;
            }
            else if (elementIsText)
            {
                elementTypeId = MetaExtractor.textID;
            }
            int elementSize = MetaExtractor.GetSizeOfField(elementTypeId);

            if (elementType == typeof(byte)&& isArray)//optimize for binary data
            {
                byte[] bytesPadded = (byte[])ByteConverter.DeserializeValueType(objectType, arrayData, checkEncrypted, dbVersion);
                byte[] realBytes = new byte[nrElem];
                Array.Copy(bytesPadded, 0, realBytes, 0, nrElem);

                return realBytes;
            }
            else if (isText)
            {
                int strNrBytes = MetaHelper.PaddingSize(nrElem);
                byte[] realBytes = new byte[strNrBytes];
                Array.Copy(arrayData, 0, realBytes, 0, strNrBytes);

                return ByteConverter.DeserializeValueType(objectType, realBytes, checkEncrypted, dbVersion);
            }
            else
            {
                Array ar = null;
                IList theList = null;
                if (isArray)
                {
                    ar = Array.CreateInstance(elementType, nrElem);
                }
                else
                {
                    object objToReturn = Activator.CreateInstance(objectType);
                    theList = (IList)objToReturn;
                }
                int currentIndex = 0;
                byte[] elemBytes = new byte[elementSize];
                for (int i = 0; i < nrElem; i++)
                {
                    Array.Copy(arrayData, currentIndex, elemBytes, 0, elementSize);
                    currentIndex += elementSize;
                    object obj = null;
                    if (elementTypeId == MetaExtractor.jaggedArrayID)
                    {
                        if (elemBytes[0] == 1)
                            obj = null;
                        else
                        {
                            byte[] nrElemeBytes = new byte[4];
                            Array.Copy(elemBytes, 1, nrElemeBytes, 0, 4);
                            int nrJaggedElem = (int)ByteConverter.DeserializeValueType(typeof(int), nrElemeBytes, dbVersion);

                            byte[] jaggedTypeIdBytes = new byte[4];
                            Array.Copy(elemBytes, nrElemeBytes.Length + 1, jaggedTypeIdBytes, 0, 4);
                            int jaggedTypeId = (int)ByteConverter.DeserializeValueType(typeof(int), jaggedTypeIdBytes, dbVersion);

                            byte[] elemeArraySizeBytes = new byte[4];
                            Array.Copy(elemBytes, nrElemeBytes.Length+jaggedTypeIdBytes.Length + 1, elemeArraySizeBytes, 0, 4);
                            int elemeArraySize = (int)ByteConverter.DeserializeValueType(typeof(int), elemeArraySizeBytes, dbVersion);

                            int jaggedElemSize=MetaExtractor.GetSizeOfField(jaggedTypeId);
                           
                            byte[]  jaggedArrayBytes = new byte[elemeArraySize-elementSize];
                           
                            Array.Copy(arrayData, currentIndex, jaggedArrayBytes, 0, jaggedArrayBytes.Length);
                            currentIndex += jaggedArrayBytes.Length;
                            obj = DeserializeArrayInternal(elementType, jaggedArrayBytes, checkEncrypted, dbVersion, isText,elementIsText, nrJaggedElem, objSerializer, parentType, fieldName,transaction);

                        }
                    }
                    else if (elementTypeId == MetaExtractor.textID)
                    {
                        if (elemBytes[0] == 1)
                            obj = null;
                        else
                        {
                            byte[] nrElemeBytes = new byte[4];
                            Array.Copy(elemBytes, 1, nrElemeBytes, 0, 4);
                            int nrJaggedElem = (int)ByteConverter.DeserializeValueType(typeof(int), nrElemeBytes, dbVersion);

                            byte[] jaggedTypeIdBytes = new byte[4];
                            Array.Copy(elemBytes, nrElemeBytes.Length + 1, jaggedTypeIdBytes, 0, 4);
                            int jaggedTypeId = (int)ByteConverter.DeserializeValueType(typeof(int), jaggedTypeIdBytes, dbVersion);

                            byte[] elemeArraySizeBytes = new byte[4];
                            Array.Copy(elemBytes, nrElemeBytes.Length + jaggedTypeIdBytes.Length + 1, elemeArraySizeBytes, 0, 4);
                            int elemeArraySize = (int)ByteConverter.DeserializeValueType(typeof(int), elemeArraySizeBytes, dbVersion);

                            
                            byte[] jaggedArrayBytes = new byte[elemeArraySize - elementSize];

                            Array.Copy(arrayData, currentIndex, jaggedArrayBytes, 0, jaggedArrayBytes.Length);
                            currentIndex += jaggedArrayBytes.Length;

                            int strNrBytes = MetaHelper.PaddingSize(nrJaggedElem);
                            byte[] realBytes = new byte[strNrBytes];
                            Array.Copy(jaggedArrayBytes, 0, realBytes, 0, strNrBytes);

                            obj = ByteConverter.DeserializeValueType(elementType, realBytes, checkEncrypted, dbVersion);

                            

                        }
                    }
                    else if (elementTypeId == MetaExtractor.complexID)
                    {
                        obj = objSerializer.ReadComplexObject(elemBytes, parentType, fieldName, transaction);
                    }
                    else
                    {
                        obj = ByteConverter.DeserializeValueType(elementType, elemBytes, checkEncrypted, dbVersion);
                    }
                    if (isArray)
                    {
                        ar.SetValue(obj, i);
                    }
                    else
                    {
                        if (obj != null)
                        {
                            theList.Add(obj);
                        }
                    }
                    

                }

                return ar==null?theList:ar;
                

            }
        }

       
        public List<KeyValuePair<int, int>> ReadComplexArrayOids( byte[] bytes, int dbVersion, ObjectSerializer objSerializer,LightningDB.LightningTransaction transaction)
        {
            return null;
            //TODO LMDB
            /*
            List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();

            if (bytes[0] == 1) //is null
            {
                return list;
            }
           
            byte[] oidBytes = new byte[4];
            Array.Copy(bytes, 1, oidBytes, 0, 4);
            int rawInfoOID = (int)ByteConverter.DeserializeValueType(typeof(int), oidBytes, dbVersion);

            byte[] nrElemeBytes = new byte[4];
            Array.Copy(bytes, MetaExtractor.ExtraSizeForArray - 4, nrElemeBytes, 0, 4);
            int nrElem = (int)ByteConverter.DeserializeValueType(typeof(int), nrElemeBytes, dbVersion);

            RawdataInfo info = manager.GetRawdataInfo(rawInfoOID,transaction);
            if (info == null)
            {
                return list;
            }

            byte[] arrayData = new byte[info.Length];
            File.Read(info.Position, arrayData);

            int currentIndex = 0;
            byte[] elemBytes = new byte[info.ElementLength];
            for (int i = 0; i < nrElem; i++)
            {
                Array.Copy(arrayData, currentIndex, elemBytes, 0, info.ElementLength);
                currentIndex += info.ElementLength;
               
                if (objSerializer != null)//complex object
                {
                    KeyValuePair<int, int> kv = objSerializer.ReadOIDAndTID(elemBytes);
                    list.Add(kv);
                }

            }
            return list;
            */
        }
        public int ReadComplexArrayFirstTID(byte[] bytes, int dbVersion, ObjectSerializer objSerializer,LightningDB.LightningTransaction transaction)
        {
            return -1;
            //TODO LMDB
            /*
           
            if (bytes[0] == 1) //is null
            {
                return -1;
            }

            byte[] oidBytes = new byte[4];
            Array.Copy(bytes, 1, oidBytes, 0, 4);
            int rawInfoOID = (int)ByteConverter.DeserializeValueType(typeof(int), oidBytes, dbVersion);

            byte[] nrElemeBytes = new byte[4];
            Array.Copy(bytes, MetaExtractor.ExtraSizeForArray - 4, nrElemeBytes, 0, 4);
            int nrElem = (int)ByteConverter.DeserializeValueType(typeof(int), nrElemeBytes, dbVersion);

            RawdataInfo info = manager.GetRawdataInfo(rawInfoOID,transaction);
            if (info == null)
            {
                return -1;
            }

            byte[] arrayData = new byte[info.Length];
            File.Read(info.Position, arrayData);

            int currentIndex = 0;
            byte[] elemBytes = new byte[info.ElementLength];
            for (int i = 0; i < nrElem; i++)
            {
                Array.Copy(arrayData, currentIndex, elemBytes, 0, info.ElementLength);
                currentIndex += info.ElementLength;

                if (objSerializer != null)//complex object
                {
                    KeyValuePair<int, int> kv = objSerializer.ReadOIDAndTID(elemBytes);
                    if (kv.Value > 0)
                    {
                        return kv.Value;
                    }
                }

            }
            return -1;
            */
        }

        private bool _transactionCommitStarted;
        internal void TransactionCommitStatus(bool started)
        {
            _transactionCommitStarted = true;
        }
        internal void Flush()
        {
            
        }

        internal void Close()
        {
            
        }
        




        internal void DeleteRawRecord(int oid, string dbName, string fieldName, LightningTransaction transaction)
        {
            try
            {
                string rawKey = oid + fieldName;
                var db = transaction.OpenDatabase(dbName, DatabaseOpenFlags.Create);

                byte[] key = ByteConverter.StringToByteArray(rawKey);
                transaction.Delete(db, key);
            }
            catch (LightningException ex)
            {
                if (!ex.Message.Contains("MDB_NOTFOUND"))
                    throw ex;
            }
        }
    }
    
}
