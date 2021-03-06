﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sqo.Meta;
using Sqo.Utilities;



namespace Sqo.Core
{
    class ArrayByteTranformer:IByteTransformer
    {
        FieldSqoInfo fi;
        SqoTypeInfo ti;
        ObjectSerializer serializer;
        RawdataSerializer rawSerializer;
         int parentOID;
         string dbName;
        public ArrayByteTranformer(ObjectSerializer serializer, RawdataSerializer rawSerializer, SqoTypeInfo ti, FieldSqoInfo fi, int parentOID)
        {
            this.serializer = serializer;
            this.ti = ti;
            this.fi = fi;
            this.rawSerializer = rawSerializer;
            this.parentOID = parentOID;
            this.dbName = string.Format("raw.{0}", ti.GetDBName());
        }
        #region IByteTransformer Members

        public byte[] GetBytes(object obj,LightningDB.LightningTransaction transaction)
        {

           
            if (fi.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.textID))
            {
                return rawSerializer.SerializeArray(obj, fi.AttributeType, fi.Header.Length, fi.Header.RealLength, ti.Header.version, dbName,fi.Name, this.serializer, true,parentOID,transaction);
            }
            else
            {
                return rawSerializer.SerializeArray(obj, fi.AttributeType, fi.Header.Length, fi.Header.RealLength, ti.Header.version, dbName,fi.Name, this.serializer, false,parentOID,transaction);
       
            }
            
        }

        public object GetObject(byte[] bytes, LightningDB.LightningTransaction transaction)
        {
            
            object fieldVal = null;
            if (fi.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.complexID) || fi.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.jaggedArrayID))// array of complexType
            {
                fieldVal = rawSerializer.DeserializeArray(fi.AttributeType, bytes, true, ti.Header.version, fi.IsText,false, this.serializer, ti.Type,dbName, fi.Name,parentOID,transaction);
            }
            else if (fi.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.textID))
            {
                fieldVal = rawSerializer.DeserializeArray(fi.AttributeType, bytes, true, ti.Header.version, false, true, this.serializer, ti.Type, dbName, fi.Name, parentOID, transaction);
            }
            else
            {

                fieldVal = rawSerializer.DeserializeArray(fi.AttributeType, bytes, true, ti.Header.version, fi.IsText, false, this.serializer, ti.Type, dbName, fi.Name, parentOID, transaction);
            }
            return fieldVal;
        }



        #endregion

    }
}
