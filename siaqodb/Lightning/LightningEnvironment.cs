﻿using System;
using System.Collections.Generic;
using System.Text;
using LightningDB.Native;

namespace LightningDB
{
    /// <summary>
    /// LMDB Environment.
    /// </summary>
    internal class LightningEnvironment : IClosingEventSource, IDisposable
    {
        private readonly UnixAccessMode _accessMode;
        private readonly EnvironmentOpenFlags _openFlags;
        internal IntPtr _handle;

        private bool _shouldDispose;

        private long _mapSize;
        private int _maxDbs;

        private readonly Dictionary<string, LightningDatabase> _openedDatabases;
        private readonly HashSet<uint> _databasesForReuse;

        /// <summary>
        /// Creates a new instance of LightningEnvironment.
        /// </summary>
        /// <param name="directory">Directory for storing database files.</param>
        /// <param name="openFlags">Database open options.</param>
        /// <param name="accessMode">Unix file access privelegies (optional). Only makes sense on unix operationg systems.</param>
        public LightningEnvironment(string directory, EnvironmentOpenFlags openFlags = EnvironmentOpenFlags.None, UnixAccessMode accessMode = UnixAccessMode.Default)
        {
            if (String.IsNullOrEmpty(directory))
                throw new ArgumentException("Invalid directory name");

            IntPtr handle = default(IntPtr);
            NativeMethods.Execute(lib => lib.mdb_env_create(out handle));

            _shouldDispose = true;
            
            _handle = handle;
            _accessMode = accessMode;

            this.Directory = directory;
            _openFlags = openFlags;

            if (LightningConfig.Environment.DefaultMapSize != LightningConfig.Environment.LibDefaultMapSize)
                this.MapSize = LightningConfig.Environment.DefaultMapSize;
            else
                _mapSize = LightningConfig.Environment.LibDefaultMapSize;

            if (LightningConfig.Environment.DefaultMaxDatabases != LightningConfig.Environment.LibDefaultMaxDatabases)
                this.MaxDatabases = LightningConfig.Environment.DefaultMaxDatabases;
            else
                _maxDbs = LightningConfig.Environment.LibDefaultMaxDatabases;

            _openedDatabases = new Dictionary<string, LightningDatabase>();
            _databasesForReuse = new HashSet<uint>();

          
        }
        private MDBStat GetStat()
        {
            EnsureOpened();

            var stat = new MDBStat();
            NativeMethods.Execute(lib => lib.mdb_env_stat(_handle, out stat));

            return stat;
        }
        public uint PageSize { get { return GetStat().ms_psize; } }

        public long UsedSize
        {
            get
            {
                var envInfo = new MDBEnvInfo();
                NativeMethods.Execute(lib => lib.mdb_env_info(_handle, out envInfo));
                this._mapSize = envInfo.me_mapsize.ToInt64();
                return envInfo.me_last_pgno.ToInt64() * PageSize;
            }
        }
        /// <summary>
        /// Triggered when the environment is going to close.
        /// </summary>
#if UNITY3D
       
        readonly object _syncRoot = new object();
        private EventHandler<LightningClosingEventArgs> closing;
        public event EventHandler<LightningClosingEventArgs> Closing
        {
            add
            {
                lock (_syncRoot)
                {
                    closing += value;
                }
            }
            remove
            {
                lock (_syncRoot)
                {
                    closing -= value;
                }
            }

        }
#else
        public event EventHandler<LightningClosingEventArgs> Closing;
#endif
        /// <summary>
        /// Whether the environment is opened.
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// Current lmdb version.
        /// </summary>
        public LightningVersionInfo Version { get { return NativeMethods.LibraryVersion; } }

        /// Set the size of the memory map to use for this environment.
        /// The size should be a multiple of the OS page size. 
        /// The default is 10485760 bytes. 
        /// The size of the memory map is also the maximum size of the database. 
        /// The value should be chosen as large as possible, to accommodate future growth of the database. 
        /// This function may only be called before the environment is opened. 
        /// The size may be changed by closing and reopening the environment. 
        /// Any attempt to set a size smaller than the space already consumed by the environment will be silently changed to the current size of the used space.
        public long MapSize
        {
            get { return _mapSize; }
            set
            {
                if (this.IsOpened)
                    throw new InvalidOperationException("Can't change MapSize of opened environment");

                if (value == _mapSize) 
                    return;

                NativeMethods.Execute(lib => lib.mdb_env_set_mapsize(_handle, value));

                _mapSize = value;
            }
        }

        /// <summary>
        /// Get the maximum number of threads for the environment.
        /// </summary>
        public int MaxReaders
        {
            get
            {
                UInt32 readers = default(UInt32);
                NativeMethods.Execute(lib => lib.mdb_env_get_maxreaders(_handle, out readers));

                return (int)readers;
            }
            set
            {
                if (this.IsOpened)
                    throw new InvalidOperationException("Can't change MaxReaders of opened environment");

                NativeMethods.Execute(lib => lib.mdb_env_set_maxreaders(_handle, (UInt32)value));
            }
        }

        /// <summary>
        /// Set the maximum number of named databases for the environment.
        /// This function is only needed if multiple databases will be used in the environment. 
        /// Simpler applications that use the environment as a single unnamed database can ignore this option. 
        /// This function may only be called before the environment is opened.
        /// </summary>
        public int MaxDatabases
        {
            get { return _maxDbs; }
            set
            {
                if (this.IsOpened)
                    throw new InvalidOperationException("Can't change MaxDatabases of opened environment");

                if (value == _maxDbs) 
                    return;

                NativeMethods.Execute(lib => lib.mdb_env_set_maxdbs(_handle, (UInt32)value));

                _maxDbs = value;
            }
        }

        /// <summary>
        /// Directory path to store database files.
        /// </summary>
        public string Directory { get; private set; }

        /// <summary>
        /// Converters to use when converting database keys and values.
        /// </summary>
       // public ConverterStore ConverterStore { get; private set; }

        /// <summary>
        /// Open the environment.
        /// </summary>
        public void Open()
        {
#if !WinRT && !NETSTANDARD
            if (!System.IO.Directory.Exists(this.Directory))
                System.IO.Directory.CreateDirectory(this.Directory);
#endif
            if (!this.IsOpened)
                NativeMethods.Execute(lib => lib.mdb_env_open(_handle, this.Directory, _openFlags, _accessMode));

            this.IsOpened = true;
        }

        /// <summary>
        /// Close the environment and release the memory map.
        /// Only a single thread may call this function. All transactions, databases, and cursors must already be closed before calling this function. 
        /// Attempts to use any such handles after calling this function will cause a SIGSEGV. 
        /// The environment handle will be freed and must not be used again after this call.
        /// </summary>
        public void Close()
        {
            if (!this.IsOpened)
                return;

            this.OnClosing();

            foreach (var hdb in _databasesForReuse)
                NativeMethods.Library.mdb_dbi_close(_handle, hdb);

            NativeMethods.Library.mdb_env_close(_handle);

            this.IsOpened = false;

            _shouldDispose = false;
        }

        /// <summary>
        /// Called when the environment is going to close.
        /// </summary>
        protected virtual void OnClosing()
        {
#if UNITY3D
            if (this.closing != null)
                this.closing(this, new LightningClosingEventArgs(true));
#else
            if (this.Closing != null)
                this.Closing(this, new LightningClosingEventArgs(true));
#endif
        }

        /// <summary>
        /// Create a transaction for use with the environment.
        /// The transaction handle may be discarded using Abort() or Commit().
        /// Note:
        /// Transactions may not span threads; a transaction must only be used by a single thread. Also, a thread may only have a single transaction.
        /// Cursors may not span transactions; each cursor must be opened and closed within a single transaction.
        /// </summary>
        /// <param name="parent">
        /// If this parameter is non-NULL, the new transaction will be a nested transaction, with the transaction indicated by parent as its parent. 
        /// Transactions may be nested to any level. 
        /// A parent transaction may not issue any other operations besides BeginTransaction, Abort, or Commit while it has active child transactions.
        /// </param>
        /// <param name="beginFlags">
        /// Special options for this transaction. 
        /// </param>
        /// <returns>
        /// New LightningTransaction
        /// </returns>
        public LightningTransaction BeginTransaction(LightningTransaction parent, TransactionBeginFlags beginFlags)
        {
            this.EnsureOpened();

            return new LightningTransaction(this, parent, beginFlags);
        }

        /// <summary>
        /// Create a transaction for use with the environment.
        /// The transaction handle may be discarded usingAbort() or Commit().
        /// Note:
        /// Transactions may not span threads; a transaction must only be used by a single thread. Also, a thread may only have a single transaction.
        /// Cursors may not span transactions; each cursor must be opened and closed within a single transaction.
        /// </summary>
        /// <param name="beginFlags">
        /// Special options for this transaction. 
        /// </param>
        /// <returns>
        /// New LightningTransaction
        /// </returns>
        public LightningTransaction BeginTransaction(TransactionBeginFlags beginFlags)
        {
            return this.BeginTransaction(null, beginFlags);
        }

        /// <summary>
        /// Create a transaction for use with the environment.
        /// The transaction handle may be discarded using Abort() or Commit().
        /// Note:
        /// Transactions may not span threads; a transaction must only be used by a single thread. Also, a thread may only have a single transaction.
        /// Cursors may not span transactions; each cursor must be opened and closed within a single transaction.
        /// </summary>        
        /// <returns>
        /// New LightningTransaction
        /// </returns>
        public LightningTransaction BeginTransaction()
        {
            return this.BeginTransaction(null, LightningTransaction.DefaultTransactionBeginFlags);
        }

        internal void ReuseDatabase(LightningDatabase db)
        {
            _openedDatabases.Remove(db.Name);
        }

        internal void ReleaseDatabase(LightningDatabase db)
        {
            _databasesForReuse.Remove(db._handle);
        }

        //TODO: Upgrade db flags?
        internal LightningDatabase OpenDatabase(string name, LightningTransaction tran, DatabaseOpenFlags? flags, Encoding encoding)
        {
            var internalName = name ?? LightningDatabase.DefaultDatabaseName;
            LightningDatabase ldb = null;
            if (_openedDatabases.ContainsKey(internalName))
            {
                ldb = _openedDatabases[internalName];
            }
            else
            {
                ldb = new LightningDatabase(name, tran, flags, encoding);
                _openedDatabases[internalName] = ldb;
            }
            _databasesForReuse.Add(ldb._handle);
           

            if (ldb.OpenFlags != flags.GetValueOrDefault())
                throw new InvalidOperationException("Database " + internalName + " already opened with different flags");

            if (ldb.Encoding != (encoding ?? LightningConfig.Database.DefaultEncoding))
                throw new InvalidOperationException("Can not change encoding of already opened database");

            return ldb;
        }

        /// <summary>
        /// Copy an MDB environment to the specified path.
        /// This function may be used to make a backup of an existing environment.
        /// </summary>
        /// <param name="path">The directory in which the copy will reside. This directory must already exist and be writable but must otherwise be empty.</param>
        public void CopyTo(string path)
        {
            this.EnsureOpened();

            NativeMethods.Execute(lib => lib.mdb_env_copy(_handle, path));
        }

        internal void ClearCachedDatabases()
        {
            this._openedDatabases.Clear();
            this._databasesForReuse.Clear();
        }

        //TODO: tests
        /// <summary>
        /// Flush the data buffers to disk. 
        /// Data is always written to disk when LightningTransaction.Commit is called, but the operating system may keep it buffered. 
        /// MDB always flushes the OS buffers upon commit as well, unless the environment was opened with EnvironmentOpenFlags.NoSync or in part EnvironmentOpenFlags.NoMetaSync.
        /// </summary>
        /// <param name="force">If true, force a synchronous flush. Otherwise if the environment has the EnvironmentOpenFlags.NoSync flag set the flushes will be omitted, and with MDB_MAPASYNC they will be asynchronous.</param>
        public void Flush(bool force)
        {
            NativeMethods.Execute(lib => lib.mdb_env_sync(_handle, force));
        }

        private void EnsureOpened()
        {
            if (!this.IsOpened)
                throw new InvalidOperationException("Environment should be opened");
        }

        /// <summary>
        /// Closes the environment and deallocates all resources associated with it.
        /// </summary>
        /// <param name="shouldDispose">True if not disposed yet.</param>
        protected virtual void Dispose(bool shouldDispose)
        {
            if (!shouldDispose || IntPtr.Zero.Equals(_handle)) 
                return;
            try
            {
                this.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Closes the environment and deallocates all resources associated with it.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(_shouldDispose);
            _shouldDispose = false;
        }
    }
}