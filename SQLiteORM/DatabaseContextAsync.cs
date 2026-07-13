using SQLiteORM.Internal;
using SQLiteORM.ORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteORM;

public partial class DatabaseContextAsync {
    public event EventHandler? DatabaseReady;
    internal class ConnectionWrapper(SQLiteConnection connection) {
        public SQLiteConnection Connection = connection;
        public bool Busy = false;
    }
    public partial class DatabaseWrapper : IDisposable {
        internal readonly ConnectionWrapper _wrapper;
        private readonly SemaphoreSlim _semaphore;
        internal DatabaseWrapper(ConnectionWrapper wrapper, SemaphoreSlim semaphore) {
            _wrapper = wrapper;
            _wrapper.Busy = true;
            this._semaphore = semaphore;
        }
        public void Dispose() {
            _wrapper.Busy = false;
            this._semaphore.Release();
        }
    }
    public partial class DatabaseWriterWrapper : DatabaseWrapper {
        public SQLiteWriteConnection Connection => (SQLiteWriteConnection)_wrapper.Connection;
        internal DatabaseWriterWrapper(ConnectionWrapper wrapper, SemaphoreSlim semaphore) : base(wrapper, semaphore) { }
    }
    public partial class DatabaseReaderWrapper : DatabaseWrapper {
        public SQLiteReadConnection Connection => (SQLiteReadConnection)_wrapper.Connection;
        internal DatabaseReaderWrapper(ConnectionWrapper wrapper, SemaphoreSlim semaphore) : base(wrapper, semaphore) { }
    }
    private SemaphoreSlim? _readSemaphore;
    private SemaphoreSlim? _writeSemaphore;
    private ConnectionWrapper? _writerConnection;
    private readonly List<ConnectionWrapper> _readerWrappers = [];
    public async Task<DatabaseReaderWrapper> GetReader() {
        if (_readSemaphore == null) {
            throw new NotInitializedException();
        }
        await _readSemaphore.WaitAsync();
        foreach (var wrapper in _readerWrappers) {
            if (!wrapper.Busy) {
                return new DatabaseReaderWrapper(wrapper, _readSemaphore);
            }
        }
        throw new Exception();
    }
    public async Task<DatabaseWriterWrapper> GetWriter() {
        if (_writeSemaphore == null || _writerConnection == null) {
            throw new NotInitializedException();
        }
        Stopwatch sw = new();
        sw.Start();
        await _writeSemaphore.WaitAsync();
        if (sw.ElapsedMilliseconds > 10) {
            Debug.WriteLine($"!!! Writer accquire delay: {sw.ElapsedMilliseconds} ms");
        }
        return new(_writerConnection, _writeSemaphore);
    }
    private async Task CreateConnections(string path, int numReader) {
        _writeSemaphore = new(1);
        _writerConnection = new(new SQLiteWriteConnection(path));
        _readSemaphore = new(numReader);
        for (int _ = 0; _ < numReader; _++) {
            _readerWrappers.Add(new(new SQLiteReadConnection(path)));
        }
        DatabaseReady?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose(bool verbose = false) {
        _writerConnection?.Connection.Dispose(verbose);
        foreach (var wrapper in _readerWrappers) {
            wrapper.Connection.Dispose(verbose);
        }
    }
    public async Task<bool> StartDatabase(string path, int numReader, List<Type> tables, List<string> onNewDatabase,  bool verbose = false) {
        bool isNew = true;
        if (File.Exists(path)) {
            isNew = false;
        }
        await CreateConnections(path, numReader);
        using (var writer = await GetWriter()) {
            if (isNew) {
                foreach (var query in onNewDatabase) {
                    await writer.Connection.ExecuteAsync(query);
                }
            }

            foreach (var type in tables) {
                string queries = TableORMConstructor.ConstructCreateTableString(type);
                foreach (var query in queries.Split(";")) {
                    if (query.Trim().Length > 0) {
                        await writer.Connection.ExecuteAsync(query + ";");
                    }
                }
            }
            if (verbose) {
                Debug.WriteLine("Table created");
            }
            return true;
        }
    }
}
