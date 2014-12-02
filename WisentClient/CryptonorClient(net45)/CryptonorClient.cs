﻿

using System.Collections.Generic;
#if ASYNC
using System.Threading.Tasks;
#endif

namespace CryptonorClient
{
    public class CryptonorClient
    {
        private CryptonorHttpClient httpClient;
        string uri;
        string username;
        string password;

        public CryptonorClient(string uri,string username,string password)
        {
            httpClient = new CryptonorHttpClient(uri, username, password);
            this.uri = uri;
            this.username = username;
            this.password = password;
#if UNITY3D || CF
          LitJson.JsonMapper.RegisterImporter<string, byte[]>(a => System.Convert.FromBase64String(a));
            LitJson.JsonMapper.RegisterExporter<byte[]>((a,b) => b.Write(System.Convert.ToBase64String(a)));
#endif
        }
        public IBucket GetBucket(string bucketName)
        {
            return new Bucket(this.uri, bucketName, this.username, this.password);
            
        }
        public IBucket GetLocalBucket(string bucketName,string localFolder)
        {
            return new LocalBucket(uri, bucketName, localFolder, this.username, this.password);
        }
#if ASYNC
        public async Task<List<string>> GetBucketsAsync()
        {
            return await httpClient.GetBucketsAsync();
            
        }
#endif
#if NON_ASYNC
        public List<string> GetBuckets()
        {
            return httpClient.GetBuckets();

        }
#endif
#if ASYNC
        public async Task<string> Ping()
        {
            return await httpClient.Ping();
            
        }
#endif
#if NON_ASYNC
        public string Ping()
        {
            return httpClient.Ping();

        }
#endif
    
    }
}