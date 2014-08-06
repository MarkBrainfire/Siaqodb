﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sqo.Attributes;
using Sqo.Exceptions;
using Sqo.Indexes;
using Sqo.Transactions;
using System.Collections;
using System.Linq.Expressions;
using System.IO;


namespace Sqo
{
    public class CryptonorLocalDB 
    {
        private Siaqodb siaqodb;
        TagsIndexManager indexManager;
        private readonly object _locker = new object();
        private readonly AsyncLock _lockerAsync = new AsyncLock();
#if !WinRT
        public CryptonorLocalDB(string bucketPath)
        {
            //SiaqodbConfigurator.EncryptedDatabase = true;
            this.siaqodb = new Siaqodb(bucketPath);
            indexManager = new TagsIndexManager(this.siaqodb);
            siaqodb.SetTagsIndexManager(indexManager);
           
        }

       
#endif
       
        private async Task CreateDirtyEntityAsync(object obj, DirtyOperation dop)
        {
            await this.CreateDirtyEntityAsync(obj, dop, null);
        }
        private async Task CreateDirtyEntityAsync(object obj, DirtyOperation dop, ITransaction transaction)
        {
            DirtyEntity dirtyEntity = new DirtyEntity();
            dirtyEntity.EntityOID = this.siaqodb.GetOID(obj);
            dirtyEntity.DirtyOp = dop;
            dirtyEntity.OperationTime = DateTime.Now;
            if (transaction != null)
            {
                await this.siaqodb.StoreObjectAsync(dirtyEntity, transaction);
            }
            else
            {
                await this.siaqodb.StoreObjectAsync(dirtyEntity);
            }
        }
       
        private async Task CreateTombstoneDirtyEntityAsync( int oid)
        {
            await this.CreateTombstoneDirtyEntityAsync( oid, null);
        }
        private async Task CreateTombstoneDirtyEntityAsync(int oid, Transaction transaction)
        {
            DirtyEntity dirtyEntity = new DirtyEntity();
            dirtyEntity.EntityOID = oid;
            dirtyEntity.DirtyOp = DirtyOperation.Deleted;
            dirtyEntity.OperationTime = DateTime.Now;
            if (transaction != null)
            {
                await this.siaqodb.StoreObjectAsync(dirtyEntity, transaction);
            }
            else
            {
                await this.siaqodb.StoreObjectAsync(dirtyEntity);
            }
        }

        public async Task Store(CryptonorObject obj)
        {
            int oID = this.siaqodb.GetOID(obj);
            if (oID == 0)
            {
                this.siaqodb.GetOIDForAMSByField(obj, "key");
            }
            oID = this.siaqodb.GetOID(obj);
            DirtyOperation dop = (oID == 0) ? DirtyOperation.Inserted : DirtyOperation.Updated;
            Dictionary<string, object> oldTags = null;
            if (dop == DirtyOperation.Updated)
            {
                oldTags = indexManager.PrepareUpdateIndexes(oID);
            }
            await this.siaqodb.StoreObjectAsync(obj);
            indexManager.UpdateIndexes(obj.OID, oldTags, obj.GetAllTags());
            if (obj.IsDirty)
            {
                await this.CreateDirtyEntityAsync(obj, dop);
            }

            this.siaqodb.Flush();

        }
      
        public async Task<IList<CryptonorObject>> LoadAll()
        {
            return await this.siaqodb.LoadAllAsync<CryptonorObject>();
        }
       
        public async Task<CryptonorObject> Load(string key)
        {
            throw new NotImplementedException();
        }
        public async Task<IList<CryptonorObject>> Load(System.Linq.Expressions.Expression expression)
        {
            return await this.siaqodb.LoadAsync<CryptonorObject>(expression);
        }
        public ISqoQuery<T> Cast<T>()
        {
            return new SqoQuery<T>(this.siaqodb);
        }
        public ISqoQuery<CryptonorObject> Query()
        {
            return new SqoQuery<CryptonorObject>(this.siaqodb);
        }
       
        private DotissiConfigurator configurator=new DotissiConfigurator();
        public DotissiConfigurator Configurator { get { return configurator; } }
        public async Task Delete(CryptonorObject cobj)
        {
            if (cobj.OID == 0)
            {
                throw new Exception("Object not exists in local database");
            }
            var oldTags = indexManager.PrepareUpdateIndexes(cobj.OID);
            await this.siaqodb.DeleteAsync(cobj);
            await this.CreateTombstoneDirtyEntityAsync(cobj.OID);
            indexManager.UpdateIndexesAfterDelete(cobj.OID, oldTags);
        }
        public async Task<ChangeSet> GetChangeSet()
        {
            IList<DirtyEntity> all=await this.siaqodb.LoadAllAsync<DirtyEntity>();
           
            Dictionary<int, Tuple<CryptonorObject, DirtyEntity>> inserts = new Dictionary<int, Tuple<CryptonorObject, DirtyEntity>>();
            Dictionary<int, Tuple<CryptonorObject, DirtyEntity>>  updates= new Dictionary<int, Tuple<CryptonorObject, DirtyEntity>>();
            Dictionary<int, Tuple<CryptonorObject, DirtyEntity>> deletes = new Dictionary<int, Tuple<CryptonorObject, DirtyEntity>>();
           
            foreach (DirtyEntity en in all)
            {

                if (en.DirtyOp == DirtyOperation.Deleted)
                {
                    if (inserts.ContainsKey(en.EntityOID))
                    {
                        await siaqodb.DeleteAsync(inserts[en.EntityOID].Item1);
                        await siaqodb.DeleteAsync(en);
                        inserts.Remove(en.EntityOID);
                        continue;
                    }
                    else if (updates.ContainsKey(en.EntityOID))
                    {
                        await siaqodb.DeleteAsync(updates[en.EntityOID].Item1);
                        updates.Remove(en.EntityOID);
                    }
                }
                else
                {
                    if (deletes.ContainsKey(en.EntityOID) || inserts.ContainsKey(en.EntityOID) || updates.ContainsKey(en.EntityOID))
                    {
                        await siaqodb.DeleteAsync(en);
                        continue;
                    }
                }


                CryptonorObject entityFromDB = (CryptonorObject)await siaqodb.LoadObjectByOIDAsync(typeof(CryptonorObject), en.EntityOID);
                if (en.DirtyOp == DirtyOperation.Inserted)
                {
                    inserts.Add(en.EntityOID, new Tuple<CryptonorObject, DirtyEntity>(entityFromDB, en));
                }
                else if (en.DirtyOp == DirtyOperation.Updated)
                {
                    updates.Add(en.EntityOID, new Tuple<CryptonorObject, DirtyEntity>(entityFromDB, en));
                }
                else if (en.DirtyOp == DirtyOperation.Deleted)
                {
                    deletes.Add(en.EntityOID, new Tuple<CryptonorObject, DirtyEntity>(entityFromDB, en));
                }

            }
            IList<CryptonorObject> changed = new List<CryptonorObject>();
            IList<DeletedObject> deleted = new List<DeletedObject>();
            foreach( Tuple<CryptonorObject,DirtyEntity> val in inserts.Values)
            {
                changed.Add(val.Item1);
            }
            foreach (Tuple<CryptonorObject, DirtyEntity> val in updates.Values)
            {
                changed.Add(val.Item1);
            }
            foreach (Tuple<CryptonorObject, DirtyEntity> val in deletes.Values)
            {
                deleted.Add(new DeletedObject { DeletedTime = val.Item2.OperationTime, Key = val.Item1.Key });
            }
            return new ChangeSet { ChangedObjects = changed,DeletedObjects=deleted };
        }

        public async Task ClearSyncMetadata()
        {
            await siaqodb.DropTypeAsync<DirtyEntity>();
        }
       
        public void Purge()
        { 
            string extension = ".sqo";
            string extension2 = ".sqr";
            if (SiaqodbConfigurator.EncryptedDatabase)
            {
                extension = ".esqo";
                extension2 = ".esqr";
            }
            DeleteFileByExt(extension);
            DeleteFileByExt(extension2);
        }
        private void DeleteFileByExt(string extension)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(siaqodb.GetDBPath());
            FileInfo[] fi = di.GetFiles("*" + extension);

            foreach (FileInfo f in fi)
            {
                File.Delete(f.FullName);
            }
        }
    }
    public class CryptonorObject
    {
        [Index]
        private string key;
        public int OID { get; set; }
       
        public bool ShouldSerializeOID()
        {
            return false;
        }
        public bool ShouldSerializeIsDirty()
        {
            return false;
        }
        public string Key
        {
            get
            {
                return this.key;
            }
            set
            {
                this.key = value;
            }
        }
        private byte[] document;
        public byte[] Document
        {
            get { return document; }
            set { document = value; }
        }
        public byte[] Version { get; set; }
       
        public bool IsDirty { get; set; }
        
        internal CryptonorObject(string key, byte[] document)
        {
            this.Key = key;
            this.Document = document;
        }
        public CryptonorObject()
        {
        }
       
        private Dictionary<string, long> tags_Int;
        public Dictionary<string, long> Tags_Int{get { return tags_Int; } set { tags_Int = value; }}

        private Dictionary<string, DateTime> tags_DateTime;
        public Dictionary<string, DateTime> Tags_DateTime { get { return tags_DateTime; } set { tags_DateTime = value; } }

        private Dictionary<string, string> tags_String;
        public Dictionary<string, string> Tags_String { get { return tags_String; } set { tags_String = value; } }

        private Dictionary<string, double> tags_Double;
        public Dictionary<string, double> Tags_Double { get { return tags_Double; } set { tags_Double = value; } }

        private Dictionary<string, bool> tags_Bool;
        public Dictionary<string, bool> Tags_Bool { get { return tags_Bool; } set { tags_Bool = value; } }

        public void SetTag(string tagName, object value)
        {
            Type type = value.GetType();
            if (type == typeof(int) || type == typeof(long))
            {
                if (Tags_Int == null)
                    Tags_Int = new Dictionary<string, long>();
                Tags_Int.Add(tagName, Convert.ToInt64(value));
            }
            else if (type == typeof(DateTime))
            {
                if (Tags_DateTime == null)
                    Tags_DateTime = new Dictionary<string, DateTime>();
                Tags_DateTime.Add(tagName, (DateTime)value);
            }

            else if (type == typeof(double) || type == typeof(float))
            {
                if (Tags_Double == null)
                    Tags_Double = new Dictionary<string, double>();
                Tags_Double.Add(tagName, Convert.ToDouble( value));
            }
            else if (type == typeof(string))
            {
                if (Tags_String == null)
                    Tags_String = new Dictionary<string, string>();
                Tags_String.Add(tagName, (string)value);
            }
          
            else if (type == typeof(bool))
            {
                if (Tags_Bool == null)
                    Tags_Bool = new Dictionary<string, bool>();
                Tags_Bool.Add(tagName, (bool)value);
            }
            else
            {
                throw new SiaqodbException("Tag type:" + type.ToString() + " not supported.");
            }
           
        }
        internal Dictionary<string, object> GetAllTags()
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            CopyDictionary(tags, this.Tags_Int);
            CopyDictionary(tags, this.Tags_String);
            CopyDictionary(tags, this.Tags_DateTime);
            CopyDictionary(tags, this.Tags_Double);
            CopyDictionary(tags, this.Tags_Bool);
            return tags;
        }
        private void CopyDictionary(Dictionary<string, object> tags, IDictionary dict_to_copy)
        {
            if (dict_to_copy != null)
            {

                foreach (string key in dict_to_copy.Keys)
                {
                    tags.Add(key, dict_to_copy[key]);
                }

            }
        }
        
    }
     enum DirtyOperation
    {
        Inserted = 1,
        Updated,
        Deleted
    }
     class DirtyEntity
    {
        public int EntityOID;
        public DirtyOperation DirtyOp;
        public DateTime OperationTime;
        public int OID
        {
            get;
            set;
        }
        public object GetValue(FieldInfo field)
        {
            return field.GetValue(this);
        }
        public void SetValue(FieldInfo field, object value)
        {
            field.SetValue(this, value);
        }
    }
    public class ChangeSet
    {
        public IList<CryptonorObject> ChangedObjects { get; internal set; }
        public IList<DeletedObject> DeletedObjects { get; internal set; }
       
    }
    public class DeletedObject
    {
        public string Key { get; set; }
        public DateTime DeletedTime { get; set; }
    }
    public class DotissiConfigurator
    {
        internal Dictionary<Type, string> buckets = new Dictionary<Type, string>();
        public void RegisterBucket(Type type, string bucketName)
        {
            buckets[type] = bucketName;
        }
    }
}
