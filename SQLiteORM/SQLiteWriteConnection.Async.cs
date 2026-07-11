using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteORM;

public partial class SQLiteWriteConnection {
    public async Task<int> ExecuteAsync(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        try {
            return await Task.Run(() => Execute(sql, args));
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task<List<object[]>> InsertAsync<T>(List<T> items, bool returnPrimaryKey) where T : class {
        await this._semaphore.WaitAsync();
        try {
            return await Task.Run(() => Insert<T>(items, returnPrimaryKey));
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task<int> DeleteAsync(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        try {
            return await Task.Run(() => Delete(sql, args));
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task<int> UpdateAsync(string sql, params object?[] args) {
        await this._semaphore.WaitAsync();
        try {
            return await Task.Run(() => Update(sql, args));
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task<int> UpdateAsync<T>(T item, string condition, params object?[] args) where T : class {
        await this._semaphore.WaitAsync();
        try {
            return await Task.Run(() => Update<T>(item, condition, args));
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task BeginTransactionAsync() {
        await this._semaphore.WaitAsync();
        try {
            await Task.Run(BeginTransaction);
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task CommitTransactionAsync() {
        await this._semaphore.WaitAsync();
        try {
            await Task.Run(CommitTransaction);
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task RollbackTransactionAsync() {
        await this._semaphore.WaitAsync();
        try {
            await Task.Run(RollbackTransaction);
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task CheckpointWALAsync() {
        await this._semaphore.WaitAsync();
        try {
            await Task.Run(CheckpointWAL);
        }
        finally {
            this._semaphore.Release();
        }
    }
}
