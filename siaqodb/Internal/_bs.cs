﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Sqo.Documents;
using Sqo.Documents.Sync;

namespace Sqo.Internal
{
    /// <summary>
    /// Do NOT use it!, it is used only internally
    /// </summary>

        public static class _bs
    {
#if !WinRT
            /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
       public static Siaqodb _b(string p)
        {
            return new Siaqodb(p, false);
        }
#endif
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Siaqodb _ofm(string p,string option)
        {
            return new Siaqodb(p, option);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _uf(Siaqodb siaqodb, int oid, MetaType metaType, string field, object value)
        {
            siaqodb.UpdateField(oid, metaType, field, value);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static List<object> _gd(Siaqodb siaqodb, Type type)
        {
            return siaqodb.LoadDirtyObjects(type);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _do(Siaqodb siaqodb, int oid, MetaType metaType)
        {
            siaqodb.DeleteObjectByMeta(oid, metaType);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static int _io(Siaqodb siaqodb,  MetaType metaType)
        {
            return siaqodb.InsertObjectByMeta(metaType);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _sdbfn(Siaqodb siaqodb, MetaType metaType,string fileName)
        {
             siaqodb.SetDatabaseFileName(fileName,metaType);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void  _loidtid(Siaqodb siaqodb,int oid, MetaType metaType, string fieldName,ref List<int> listOIDs,ref int TID)
        {
             siaqodb.LoadObjectOIDAndTID(oid, fieldName, metaType,ref listOIDs,ref TID);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _ltid(Siaqodb siaqodb, int oid, MetaType metaType, string fieldName, ref int TID,ref bool isArray)
        {
            siaqodb.LoadTIDofComplex(oid, fieldName, metaType, ref TID, ref isArray);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _loidby(Siaqodb siaqodb, string fieldName, object obj)
        {
            siaqodb.GetOIDForAMSByField(obj, fieldName);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static object _lobjby(Siaqodb siaqodb, Type type, int oid)
        {
            return siaqodb.LoadObjectByOID(type, oid);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static bool _hsy()
        {
            return Sqo.Utilities.SqoLicense.hasSync;
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static bool _hamssy()
        {
            return Sqo.Utilities.SqoLicense.hasAMSSync;
        }
              /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _sanc(Siaqodb siaqodb, byte[] b, string k)
        {
            siaqodb.SaveAnchor(k, b);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static byte[] _ganc(Siaqodb siaqodb, string k)
        {
            return siaqodb.GetAnchor(k);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _danc(Siaqodb siaqodb, string k)
        {
            siaqodb.DropAnchor(k);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static ChangeSet _gcs(Bucket b)
        {
            return b.GetChangeSet();
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _uv(Bucket b,IEnumerable<KeyValuePair<string, string>> su)
        {
            b.UpdateVersions(su);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _csm(Bucket b)
        {
            b.ClearSyncMetadata();
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _de(Bucket b,string k,bool d)
        {
            b.Delete(k, d);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _sa(Bucket b, string a)
        {
            b.StoreAnchor(a);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _sb(Bucket b,IList<Document> dcs, bool d)
        {
            b.StoreBatch(dcs,d);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static string _gab(Bucket b)
        {
           return b.GetAnchor();
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static bool _ibs(Bucket b)
        {
            return Sqo.SiaqodbConfigurator.IsBucketSyncable(b.BucketName);
        }
        /// <summary>
        /// Do NOT use it!, it is used only internally
        /// </summary>
        public static void _si(Bucket b, Document d,bool id)
        {
            b.StoreInternal(d, id);
        }

    }
}
