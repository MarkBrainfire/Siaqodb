﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqo;
using TestSiaqodbBuckets;
using System.Text;
using SiaqodbCloud;
using Sqo.Documents;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;

namespace TestSiaqodbBuckets
{
    [TestClass]
    public class SiaqodbCloudSync
    {
        Siaqodb siaqodb1;
        Siaqodb siaqodb2;
        Random rnd = new Random();
        public SiaqodbCloudSync()
        {
            Sqo.SiaqodbConfigurator.SetLicense(@" vxkmLEjihI7X+S2ottoS2cVvZnIPY8dL9wyf3RMWpjKO0WGBVXmnDc82AKBClJ/u");
            Sqo.SiaqodbConfigurator.SetSyncableBucket("contacts", true);
            Sqo.SiaqodbConfigurator.SetSyncableBucket("personas", true);
            Sqo.SiaqodbConfigurator.SetSyncableBucket("personas1", true);
            Sqo.SiaqodbConfigurator.SetDocumentSerializer(new MyJsonSerializer());
            this.siaqodb1 = new Siaqodb(@"c:\work\temp\buk_tests\sync1\");
            this.siaqodb2 = new Siaqodb(@"c:\work\temp\buk_tests\sync2\");
        }
        private SiaqodbSync GetSyncContext()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            return new SiaqodbSync(tableClient);
            //return new SiaqodbSync("http://localhost:11735/v0/", "97c7835153fd66617fad7b43f4000647", "4362kljh63k4599hhgm");
        }
        [TestMethod]
        public void Insert()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas1"];
                IBucket bucket2 = siaqodb2.Documents["personas1"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");
                   
                    Filter f = new Filter("key");
                    f.Value = userName;
                    var presult = syncContext.Pull(bucket2,f);
                    var fromDB = bucket2.Load(userName);
                    Assert.AreEqual(22, fromDB.GetTag<int>("Age"));
                    var value = fromDB.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(p.FirstName, value.FirstName);
                }
            }
        }

        [TestMethod]
        public void Update()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter query = new Filter("key");
                    query.Value = userName;
                    syncContext.Pull(bucket, query);


                    var fromDB1 = bucket.Load(userName);
                    var value = fromDB1.GetContent<Person>();
                    value.FirstName = "Alisia";
                    value.Age = 44;
                    fromDB1.SetContent<Person>(value);
                    fromDB1.SetTag("Age", 44);
                    bucket.Store(fromDB1);

                    syncContext.Pull(bucket, query);//also calls Push()

                    fromDB1 = bucket.Load(userName);
                    Assert.AreEqual(44, fromDB1.GetTag<int>("Age"));
                    value = fromDB1.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(44, value.Age);
                    Assert.AreEqual("Alisia", value.FirstName);

                    syncContext.Pull(bucket2, query);

                    fromDB1 = bucket2.Load(userName);
                    Assert.AreEqual(44, fromDB1.GetTag<int>("Age"));
                    value = fromDB1.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(44, value.Age);
                    Assert.AreEqual("Alisia", value.FirstName);
                }
            }
        }
        [TestMethod]
        public void Delete()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter query = new Filter("key");
                    query.Value=userName;
                    syncContext.Pull(bucket, query);

                    var fromDB1 = bucket.Load(userName);

                    bucket.Delete(fromDB1);

                    syncContext.Pull(bucket, query);//also calls Push()

                    fromDB1 = bucket.Load(userName);
                    Assert.IsNull(fromDB1);

                    syncContext.Pull(bucket2, query);

                    fromDB1 = bucket2.Load(userName);
                    Assert.IsNull(fromDB1);
                }
            }
        }
        [TestMethod]
        public void TestConflictUpdateUpdate()
        {//update on C1 and C2 at same time->should be conflict
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter query = new Filter("key");
                    query.Value = userName;
                    syncContext.Pull(bucket, query);


                    syncContext.Pull(bucket2, query);
                    //now both databases has same object with same version
                     
                    var fromDB1 = bucket.Load(userName);
                    var value = fromDB1.GetContent<Person>();
                    value.FirstName = "Alisia";
                    value.Age = 44;
                    fromDB1.SetContent<Person>(value);
                    fromDB1.SetTag("Age", 44);
                    bucket.Store(fromDB1);

                    fromDB1 = bucket2.Load(userName);
                    value = fromDB1.GetContent<Person>();
                    value.FirstName = "Alisia22";
                    value.Age = 6;
                    fromDB1.SetContent<Person>(value);
                    fromDB1.SetTag("Age", 6);
                    bucket2.Store(fromDB1);

                    syncContext.Pull(bucket, query);//also calls Push()

                    syncContext.Pull(bucket2, query);//also calls Push()

                    fromDB1 = bucket.Load(userName);
                    Assert.AreEqual(44, fromDB1.GetTag<int>("Age"));
                    value = fromDB1.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(44, value.Age);
                    Assert.AreEqual("Alisia", value.FirstName);

                    fromDB1 = bucket2.Load(userName);//get from bucket2->it must be conflicted

                    Assert.AreEqual(44, fromDB1.GetTag<int>("Age"));
                    value = fromDB1.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(44, value.Age);
                    Assert.AreEqual("Alisia", value.FirstName);
                }
            }
        }


        [TestMethod]
        public void TestConflictDeleteUpdate()
        {//delete on C1 and update on C2->conflict
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {



                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");
                    Filter query = new Filter("key");
                    query.Value = userName;
                    var firstpull = syncContext.Pull(bucket, query);


                    var pl2 = syncContext.Pull(bucket2, query);
                    //now both databases has same object with same version

                    var fromDB1 = bucket.Load(userName);

                    bucket.Delete(fromDB1);
                    var pl = syncContext.Pull(bucket, query);//also calls Push()

                    fromDB1 = bucket.Load(userName);
                    Assert.IsNull(fromDB1);

                    fromDB1 = bucket2.Load(userName);
                    var value = fromDB1.GetContent<Person>();
                    value.FirstName = "Alisia22";
                    value.Age = 6;
                    fromDB1.SetContent<Person>(value);
                    fromDB1.SetTag("Age", 6);
                    bucket2.Store(fromDB1);

                    syncContext.Pull(bucket2, query);//also calls Push()

                    fromDB1 = bucket2.Load(userName);
                    Assert.IsNull(fromDB1);
                }
            }
        }


        [TestMethod]
        public void TestConflictUpdateDelete()
        {
            //update on C1 and delete on C2->conflict
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter query = new Filter("key");
                    query.Value = userName;
                    syncContext.Pull(bucket, query);


                    syncContext.Pull(bucket2, query);
                    //now both databases has same object with same version

                    var fromDB1 = bucket.Load(userName);
                    var value = fromDB1.GetContent<Person>();
                    value.FirstName = "Alisia22";
                    value.Age = 6;
                    fromDB1.SetContent<Person>(value);
                    fromDB1.SetTag("Age", 6);
                    bucket.Store(fromDB1);
                    syncContext.Pull(bucket, query);//also calls Push()

                    fromDB1 = bucket2.Load(userName);
                    bucket2.Delete(fromDB1);
                    syncContext.Pull(bucket2, query);

                    fromDB1 = bucket2.Load(userName);
                    Assert.AreEqual(6, fromDB1.GetTag<int>("Age"));
                    value = fromDB1.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(6, value.Age);
                    Assert.AreEqual("Alisia22", value.FirstName);
                }
            }
        }


        [TestMethod]
        public void TestConflictDeleteDelete()
        {
            //delete on C1 and delete on C2->conflict-> nothing happens on clients
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");
                    Filter query = new Filter("key");
                    query.Value = userName;
                    var pl = syncContext.Pull(bucket, query);


                    syncContext.Pull(bucket2, query);
                    //now both databases has same object with same version

                    var fromDB1 = bucket.Load(userName);

                    bucket.Delete(fromDB1);
                    syncContext.Pull(bucket, query);//also calls Push()

                    fromDB1 = bucket.Load(userName);
                    Assert.IsNull(fromDB1);

                    fromDB1 = bucket2.Load(userName);
                    bucket2.Delete(fromDB1);
                    syncContext.Pull(bucket2, query);//also calls Push()

                    fromDB1 = bucket2.Load(userName);
                    Assert.IsNull(fromDB1);
                }
            }
        }
        private class MergeResolver : IConflictResolver
        {
            public Document Resolve(Document local, Document online)
            {
                var localPers = local.GetContent<Person>();
                var livePers = online.GetContent<Person>();
                localPers.LastName = livePers.LastName;
                local.SetContent(localPers);
                return local;
            }
        }

        [TestMethod]
        public void TestConflictConventionMergeUpdate()
        {
          
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {
                    int rndNr = rnd.Next(100000000);
                    string userName = "user" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    // Syncing the local and live buckets

                    Filter query = new Filter("key");
                    query.Value = userName;

                    var result = syncContext.Pull(bucket, query);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.PushResult.Conflicts != null && result.PushResult.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    syncContext.Pull(bucket2, query);

                    //update data bucket1
                    var localVersion = bucket.Load(p.UserName);
                    p = localVersion.GetContent<Person>();
                    p.FirstName = "Irinel";
                    p.LastName = "Nistor";
                    localVersion.SetContent(p);
                    bucket.Store(localVersion);



                    //update the live version in order to get a conflict
                    var liveVersion = bucket2.Load(userName);
                    var person = liveVersion.GetContent<Person>();
                    person.FirstName = "Lucian";
                    person.LastName = "Norocel";
                    liveVersion.SetContent(person);
                    bucket2.Store(liveVersion);

                    syncContext.Pull(bucket2, query);


                    //Sync the live and the local version
                    syncContext.Pull(bucket, query, new MergeResolver());//also calls Push()

                    syncContext.Pull(bucket2, query);

                    liveVersion = bucket2.Load(userName);
                    person = liveVersion.GetContent<Person>();

                    localVersion = bucket.Load(userName);
                    var localPerson = localVersion.GetContent<Person>();
                    //the live object should be the same as the one stored local
                    Assert.AreEqual(person.LastName, localPerson.LastName);
                    Assert.AreEqual(person.LastName, "Norocel");
                    Assert.AreEqual(person.FirstName, localPerson.FirstName);
                    Assert.AreEqual(person.FirstName, "Irinel");
                }
            }
        }
        private class LocalResolver : IConflictResolver
        {
            public Document Resolve(Document local, Document online)
            {
               
                return local;
            }
        }
        //Test the conflict convention with the local bucket as winner
        [TestMethod]
        public void TestConflictConventionLocalUpdate()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {
                   
                    int rndNr = rnd.Next(100000000);
                    string userName = "user" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    // Syncing the local and live buckets

                    Filter query = new Filter("key");
                    query.Value = userName;

                    var result = syncContext.Pull(bucket, query);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.PushResult.Conflicts != null && result.PushResult.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    syncContext.Pull(bucket2, query);

                    //update the local version
                    var localVersion = bucket.Load(p.UserName);
                    p = localVersion.GetContent<Person>();
                    p.FirstName = "Irinel";
                    localVersion.SetContent(p);
                    bucket.Store(localVersion);

                    //update the live version in order to get a conflict
                    var liveVersion = bucket2.Load(userName);
                    var person = liveVersion.GetContent<Person>();
                    person.FirstName = "Lucian";
                    liveVersion.SetContent(person);
                    bucket2.Store(liveVersion);
                    syncContext.Pull(bucket2, query);

                    //Sync the live and the local version
                    syncContext.Pull(bucket, query, new LocalResolver());//also calls Push()
                    syncContext.Pull(bucket2, query);

                    liveVersion = bucket2.Load(userName);
                    person = liveVersion.GetContent<Person>();

                    //the live object should be the same as the one stored local
                    Assert.AreEqual(person.FirstName, p.FirstName);
                }
            }
        }
        //Test the conflict convention with the local bucket as winner
        [TestMethod]
        public void TestConflictConventionLocalDelete()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {
                    
                    int rndNr = rnd.Next(100000000);
                    string userName = "user" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    // Syncing the local and live buckets

                    Filter query = new Filter("key");
                    query.Value = userName;

                    var result = syncContext.Pull(bucket, query);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.PushResult.Conflicts != null && result.PushResult.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    syncContext.Pull(bucket2, query);

                    //delete the local version
                    bucket.Delete(p.UserName);

                    //update the live version in order to get a conflict
                    var liveVersion = bucket2.Load(userName);
                    var person = liveVersion.GetContent<Person>();
                    person.FirstName = "Lucian";
                    liveVersion.SetContent(person);
                    bucket2.Store(liveVersion);
                    syncContext.Pull(bucket2, query);

                    //Sync the live and the local version
                    syncContext.Pull(bucket, query, new LocalResolver());//also calls Push()
                    syncContext.Pull(bucket2, query);

                    liveVersion = bucket2.Load(userName);

                    //the live object should be the same as the one stored local
                    Assert.IsNull(liveVersion);
                }
            }
        }
        private class LiveResolver : IConflictResolver
        {
            public Document Resolve(Document local, Document online)
            {

                return online;
            }
        }
        //Test the conflict convention with the live bucket as winner
        [TestMethod]
        public void TestConflictConventionLiveUpdate()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {
                    int rndNr = rnd.Next(100000000);
                    string userName = "user" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    // Syncing the local and live buckets

                    Filter query = new Filter("key");
                    query.Value = userName;

                    var result = syncContext.Pull(bucket, query);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.PushResult.Conflicts != null && result.PushResult.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    syncContext.Pull(bucket2, query);


                    //update the live version in order to get a conflict
                    var liveVersion = bucket2.Load(userName);
                    var person = liveVersion.GetContent<Person>();
                    person.FirstName = "Lucian";
                    liveVersion.SetContent(person);
                    bucket2.Store(liveVersion);
                    syncContext.Pull(bucket2, query);

                    //update the local version
                    var localVersion = bucket.Load(p.UserName);
                    p = localVersion.GetContent<Person>();
                    p.FirstName = "Irinel";
                    localVersion.SetContent(p);
                    bucket.Store(localVersion);


                    //Sync the live and the local version
                    syncContext.Pull(bucket, query, new LiveResolver());//also calls Push()
                    syncContext.Pull(bucket2, query);

                    localVersion = bucket.Load(userName);
                    p = localVersion.GetContent<Person>();

                    //the live object should be the same as the one stored local
                    Assert.AreEqual(person.FirstName, p.FirstName);
                }
            }
        }
        //Test the conflict convention with the live bucket as winner
        [TestMethod]
        public void TestConflictConventionLiveDelete()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas"];
                IBucket bucket2 = siaqodb2.Documents["personas"];
                {
                   
                    int rndNr = rnd.Next(100000000);
                    string userName = "user" + rndNr;
                    Person p = GetPerson(userName);
                    bucket.Store(p.UserName, p, new { Age = 22 });
                    // Syncing the local and live buckets

                    Filter query = new Filter("key");
                    query.Value = userName;

                    var result = syncContext.Pull(bucket, query);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.PushResult.Conflicts != null && result.PushResult.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");


                    syncContext.Pull(bucket2, query);


                    //delete the live version in order to get a conflict
                    bucket2.Delete(userName);
                    syncContext.Pull(bucket2, query);

                    //update the local version
                    var localVersion = bucket.Load(p.UserName);
                    p = localVersion.GetContent<Person>();
                    p.FirstName = "Irinel";
                    localVersion.SetContent(p);
                    bucket.Store(localVersion);

                    //Sync the live and the local version
                    syncContext.Pull(bucket, query, new LiveResolver());//also calls Push()
                    
                    localVersion = bucket.Load(p.UserName);

                    //the live object should be the same as the one stored local
                    Assert.IsNull(localVersion);
                }
            }
        }
        [TestMethod]
        public void TestIntFilters()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas1"];
                IBucket bucket2 = siaqodb2.Documents["personas1"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    int myint = rndNr;
                    double myFloat = (double)rndNr + 0.55;
                    DateTime myDate = new DateTime(2016, rndNr%10+1, rndNr%28+1);
                    string myStr = rndNr.ToString();
                    bool myB = true;
                    bucket.Store(p.UserName, p, new { Myint = myint, MyFloat = myFloat, MyDate = myDate, MyStr = myStr, MyBool = myB });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter f = new Filter("myint");
                    f.Value = rndNr;
                    var presult = syncContext.Pull(bucket2, f);
                    var fromDB = bucket2.Load(userName);
                    Assert.AreEqual(rndNr, fromDB.GetTag<int>("myint"));
                    var value = fromDB.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(p.FirstName, value.FirstName);

                    f = new Filter("myint");
                    f.Start = rndNr;
                    var presultStart = syncContext.Pull(bucket2, f);
                    Query q = new Query();
                    q.WhereGreaterThanOrEqual("myint",rndNr);
                    var all = bucket2.Find(q);
                    Assert.IsTrue(all.Count > 0);
                    foreach (Document d in all)
                    {
                        Assert.IsTrue(fromDB.GetTag<int>("myint") >= rndNr);
                    }

                    f = new Filter("myint");
                    f.End = rndNr;
                    var presulEnd = syncContext.Pull(bucket2, f);
                    Query ql = new Query();
                    ql.WhereLessThanOrEqual("myint", rndNr);
                    var allEnd = bucket2.Find(ql);
                    Assert.IsTrue(allEnd.Count > 0);
                    foreach (Document d in allEnd)
                    {
                        Assert.IsTrue(fromDB.GetTag<int>("myint") <= rndNr);
                    }
                    f = new Filter("myint");
                    f.Start = rndNr;
                    f.End = rndNr;
                    var psresult = syncContext.Pull(bucket2, f);
                    Assert.IsTrue(psresult.SyncStatistics.TotalDownloads == 0);

                }
            }
        }
        [TestMethod]
        public void TestFloatFilters()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas1"];
                IBucket bucket2 = siaqodb2.Documents["personas1"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    int myint = rndNr;
                    double myFloat = (double)rndNr + 0.55;
                    DateTime myDate = new DateTime(2016, rndNr % 10 + 1, rndNr % 28 + 1);
                    string myStr = rndNr.ToString();
                    bool myB = true;
                    bucket.Store(p.UserName, p, new { Myint = myint, MyFloat = myFloat, MyDate = myDate, MyStr = myStr, MyBool = myB });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter f = new Filter("myfloat");
                    f.Value = myFloat;
                    var presult = syncContext.Pull(bucket2, f);
                    var fromDB = bucket2.Load(userName);
                    Assert.AreEqual(myFloat, fromDB.GetTag<double>("myFloat"));
                    var value = fromDB.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(p.FirstName, value.FirstName);

                    f = new Filter("myfloat");
                    f.Start = myFloat;
                    var presultStart = syncContext.Pull(bucket2, f);
                    Query q = new Query();
                    q.WhereGreaterThanOrEqual("myfloat", myFloat);
                    var all = bucket2.Find(q);
                    Assert.IsTrue(all.Count > 0);
                    foreach (Document d in all)
                    {
                        Assert.IsTrue(fromDB.GetTag<double>("myfloat") >= myFloat);
                    }

                    f = new Filter("myfloat");
                    f.End = myFloat;
                    var presulEnd = syncContext.Pull(bucket2, f);
                    Query ql = new Query();
                    ql.WhereLessThanOrEqual("myfloat", myFloat);
                    var allEnd = bucket2.Find(ql);
                    Assert.IsTrue(allEnd.Count > 0);
                    foreach (Document d in allEnd)
                    {
                        Assert.IsTrue(fromDB.GetTag<double>("myfloat") <= myFloat);
                    }
                    f = new Filter("myfloat");
                    f.Start = myFloat;
                    f.End = myFloat;
                    var psresult = syncContext.Pull(bucket2, f);
                    Assert.IsTrue(psresult.SyncStatistics.TotalDownloads == 0);

                }
            }
        }
        [TestMethod]
        public void TestDateTimeFilters()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas1"];
                IBucket bucket2 = siaqodb2.Documents["personas1"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    int myint = rndNr;
                    double myFloat = (double)rndNr + 0.55;
                    DateTime myDate = new DateTime(2016, rndNr % 10 + 1, rndNr % 28 + 1);
                    myDate = DateTime.SpecifyKind(myDate, DateTimeKind.Utc);
                    string myStr = rndNr.ToString();
                    bool myB = true;
                    bucket.Store(p.UserName, p, new { Myint = myint, MyFloat = myFloat, MyDate = myDate, MyStr = myStr, MyBool = myB });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter f = new Filter("mydate");
                    f.Value = myDate;
                    var presult = syncContext.Pull(bucket2, f);
                    var fromDB = bucket2.Load(userName);
                    Assert.AreEqual(myDate, fromDB.GetTag<DateTime>("mydate"));
                    var value = fromDB.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(p.FirstName, value.FirstName);

                    f = new Filter("mydate");
                    f.Start = myDate;
                    var presultStart = syncContext.Pull(bucket2, f);
                    Query q = new Query();
                    q.WhereGreaterThanOrEqual("mydate", myDate);
                    var all = bucket2.Find(q);
                    Assert.IsTrue(all.Count > 0);
                    foreach (Document d in all)
                    {
                        Assert.IsTrue(fromDB.GetTag<DateTime>("mydate") >= myDate);
                    }

                    f = new Filter("mydate");
                    f.End = myDate;
                    var presulEnd = syncContext.Pull(bucket2, f);
                    Query ql = new Query();
                    ql.WhereLessThanOrEqual("mydate", myDate);
                    var allEnd = bucket2.Find(ql);
                    Assert.IsTrue(allEnd.Count > 0);
                    foreach (Document d in allEnd)
                    {
                        Assert.IsTrue(fromDB.GetTag<DateTime>("mydate") <= myDate);
                    }
                    f = new Filter("mydate");
                    f.Start = myDate;
                    f.End = myDate;
                    var psresult = syncContext.Pull(bucket2, f);
                    Assert.IsTrue(psresult.SyncStatistics.TotalDownloads == 0);

                }
            }
        }
        [TestMethod]
        public void TestStringFilters()
        {
            using (SiaqodbSync syncContext = this.GetSyncContext())
            {
                IBucket bucket = siaqodb1.Documents["personas1"];
                IBucket bucket2 = siaqodb2.Documents["personas1"];
                {

                    int rndNr = rnd.Next(100000000);
                    string userName = "userName" + rndNr;
                    Person p = GetPerson(userName);
                    int myint = rndNr;
                    double myFloat = (double)rndNr + 0.55;
                    DateTime myDate = new DateTime(2016, rndNr % 10 + 1, rndNr % 28 + 1);
                    string myStr = rndNr.ToString();
                    bool myB = true;
                    bucket.Store(p.UserName, p, new { Myint = myint, MyFloat = myFloat, MyDate = myDate, MyStr = myStr, MyBool = myB });
                    var result = syncContext.Push(bucket);
                    if (result.Error != null)
                        throw result.Error;
                    if (result.Conflicts != null && result.Conflicts.Count > 0)
                        throw new Exception("Random not OK retry");

                    Filter f = new Filter("mystr");
                    f.Value = myStr;
                    var presult = syncContext.Pull(bucket2, f);
                    var fromDB = bucket2.Load(userName);
                    Assert.AreEqual(myStr, fromDB.GetTag<string>("mystr"));
                    var value = fromDB.GetContent<Person>();
                    Assert.AreEqual(userName, value.UserName);
                    Assert.AreEqual(p.FirstName, value.FirstName);

                    f = new Filter("mystr");
                    f.Start = myStr;
                    var presultStart = syncContext.Pull(bucket2, f);
                    Query q = new Query();
                    q.WhereGreaterThanOrEqual("mystr", myStr);
                    var all = bucket2.Find(q);
                    Assert.IsTrue(all.Count > 0);
                    foreach (Document d in all)
                    {
                        Assert.IsTrue(string.Compare( fromDB.GetTag<string>("mystr"), myStr)>=0);
                    }

                    f = new Filter("mystr");
                    f.End = myStr;
                    var presulEnd = syncContext.Pull(bucket2, f);
                    Query ql = new Query();
                    ql.WhereLessThanOrEqual("mystr", myStr);
                    var allEnd = bucket2.Find(ql);
                    Assert.IsTrue(allEnd.Count > 0);
                    foreach (Document d in allEnd)
                    {
                        Assert.IsTrue(string.Compare(fromDB.GetTag<string>("mystr"), myStr)<=0);
                    }
                    f = new Filter("mystr");
                    f.Start = myStr;
                    f.End = myStr;
                    var psresult = syncContext.Pull(bucket2, f);
                    Assert.IsTrue(psresult.SyncStatistics.TotalDownloads == 0);

                }
            }
        }
        public string GetRandomString(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }
        public Person GetPerson(string userName)
        {
            Person p = new Person() { UserName = userName };
            p.Email = userName + "@gmail.com";
            p.Age = 22;
            p.BirthDate = new DateTime(1981, 1, 4);
            p.FirstName = "Cristi";
            p.LastName = "Ursachi";
            return p;
        }
    }
}
