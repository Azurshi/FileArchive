using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class EntryRepository {
    private readonly DatabaseContextAsync _db;
    public EntryRepository(DataManager manager) {
        this._db = manager.Database;
    }
    public async Task<EntryEntity?> Get(long entryId) {
        using (var db = await this._db.GetReader()) {
            return await GetEntry(db.Connection, entryId);
        }
    }
    public async Task<List<EntryEntity>> GetChildren(long entryId) {
        using (var db = await this._db.GetReader()) {
            return await GetChildrenAsync(db.Connection, entryId);
        }
    }
    public async Task<bool> Rename(long entryId, string newName) {
        using (var db = await this._db.GetWriter()) {
            var connection = db.Connection;
            if (await CheckExistsAsync(connection, entryId)
                && !await ContainAsync(connection, entryId, newName)
                ) {
                var _ = await connection.ExecuteAsync(
                    "UPDATE EntryEntity SET Name = ? WHERE Id = ?", newName, entryId);
                return true;
            }
            else {
                return false;
            }
        }
    }
    public async Task<EntryEntity?> CreateFolder(long parentId, string folderName) {
        using (var db = await this._db.GetWriter()) {
            var connection = db.Connection;
            if (!await ContainAsync(connection, parentId, folderName)) {
                var now = DateTime.Now;
                int attributes = (int)EntryAttributes.Directory;
                EntryEntity entity = new((-1, parentId, attributes, 0, now, now, now, now, folderName, string.Empty));
                List<object[]> ids = await db.Connection.InsertAsync([entity], true);
                if (ids.Count > 0) {
                    long id = (long)ids[0][0];
                    return entity.WithId(id);
                }
                else {
                    return null;
                }
            }
            else {
                return null;
            }
        }
    }
    public async Task<List<string>> GetPath(long entryId) {
        List<string> result = [];
        using (var db = await _db.GetReader()) {
            var connection = db.Connection;
            while (entryId != -1) {
                var rows = await connection.SelectAsync<string, long>(
                    "SELECT Name, Id FROM EntryEntity WHERE Id = ?", entryId);
                if (rows.Count > 0) {
                    entryId = rows[0].Item2;
                    result.Insert(0, rows[0].Item1);
                }
            }
        }
        return result;
    }
    public async Task<bool> Contain(long entryId, long childrenId) {
        using (var db = await _db.GetReader()) {
            return await Contain(db.Connection, entryId, childrenId);
        }
    }
    public async Task<bool> Move(long entryId, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await this._db.GetWriter()) {
            var connection = db.Connection;
            if (await CheckExistsAsync(connection, entryId)
                && await CheckExistsAsync(connection, toFolderId)
                && !await Contain(connection, toFolderId, entryId)
                ) {
                var _ = await connection.UpdateAsync("""
                    UPDATE EntryEntity
                    SET ParentId = ?
                    WHERE Id = ?
                    """, toFolderId, entryId);
                return true;
            }
            else {
                return false;
            }
        }
    }
    public async Task<bool> Copy(long entryId, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await this._db.GetWriter()) {
            var connection = db.Connection;
            if ((await GetEntry(connection, entryId)) is EntryEntity rootEntry
                && await CheckExistsAsync(connection, toFolderId)
                && !await Contain(connection, toFolderId, entryId)
                ) {
                await connection.BeginTransactionAsync();
                try {
                    rootEntry = rootEntry.WithIdAndParent(-1, toFolderId);
                    var newRootEntry = (await CopyAndReparent(connection, [rootEntry], toFolderId))[0];
                    var id = newRootEntry.Id!.Value;
                    // Parent source (old id), Parent dst (new id)
                    Queue<ValueTuple<long, long>> q = new();
                    q.Enqueue((entryId, id));
                    while (q.Count > 0) {
                        var (parentOldId, parentNewId) = q.Dequeue();
                        var children = await GetChildrenAsync(connection, parentOldId);
                        var copiedChildren = await CopyAndReparent(connection, children, parentNewId);
                        for(int i=0; i<copiedChildren.Count; i++) {
                            id = copiedChildren[i].Id!.Value;
                            var oldId = children[i].Id!.Value;
                            q.Enqueue((oldId, id));
                        }
                    }
                    await connection.CommitTransactionAsync();
                    return true;
                }
                catch {
                    await connection.RollbackTransactionAsync();
                    return false;
                }
            }
            else {
                return false;
            }
        }
    }
}
