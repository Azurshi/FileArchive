using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Search;

namespace AzurArchive.Data.Database.Repositories;

internal partial class EntryRepository {
    private readonly DatabaseContextAsync _db;
    private readonly DatabaseContext _syncDb;
    public EntryRepository(DataManager manager) {
        this._db = manager.Database;
        this._syncDb = manager.SyncDatabase;
    }
    public async Task<EntryEntity?> Get(long entryId) {
        using (var db = await this._db.GetReader()) {
            return await GetEntryAsync(db.Connection, entryId);
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
                EntryEntity entity = new((-1, parentId, attributes, 0, now, now, now, folderName, null, string.Empty));
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
        using (var db = await _db.GetReader()) {
            return await GetPath(db.Connection, entryId);
        }
    }
    public async Task<bool> ContainAsync(long entryId, long childrenId) {
        using (var db = await _db.GetReader()) {
            return await ContainAsync(db.Connection, entryId, childrenId);
        }
    }
    public async Task<bool> Move(List<long> entryIds, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await this._db.GetWriter()) {
            var connection = db.Connection;
            await connection.BeginTransactionAsync();
            try {
                foreach (var entryId in entryIds) {
                    if (await ContainRecursiveAsync(connection, entryId, toFolderId)) {
                        throw new Exception("Destination is inside source");
                    }
                    else if (await GetEntryAsync(connection, entryId) is EntryEntity entry
                        && (toFolderId == -1 || await CheckExistsAsync(connection, toFolderId))
                        && !await ContainAsync(connection, toFolderId, entryId)
                        ) {
                        //entry = ClearTrashMetadata(entry);
                        var _ = await connection.UpdateAsync("""
                            UPDATE EntryEntity
                            SET ParentId = ?,
                                ExtendedData = ?
                            WHERE Id = ?
                            """, toFolderId, entry.ExtendedData, entryId);
                    }
                    else {
                        throw new Exception("Entry not exists or already contain entry with same name");
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
    }
    public async Task<bool> Copy(List<long> entryIds, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await this._db.GetWriter()) {
            var connection = db.Connection;
            await connection.BeginTransactionAsync();
            try {
                foreach (var entryId in entryIds) {
                    if (await ContainRecursiveAsync(connection, entryId, toFolderId)) {
                        throw new Exception("Destination is inside source");
                    }
                    else if ((await GetEntryAsync(connection, entryId)) is EntryEntity rootEntry
                        && (toFolderId == -1 || await CheckExistsAsync(connection, toFolderId))
                        && !await ContainAsync(connection, toFolderId, entryId)
                        ) {
                        //rootEntry = ClearTrashMetadata(rootEntry);
                        var newRootEntry = (await CopyAndReparent(connection, [rootEntry], toFolderId))[0];
                        var id = newRootEntry.Id!.Value;
                        // Parent source (old id), Parent dst (new id)
                        Queue<ValueTuple<long, long>> q = new();
                        q.Enqueue((entryId, id));
                        while (q.Count > 0) {
                            var (parentOldId, parentNewId) = q.Dequeue();
                            var children = await GetChildrenAsync(connection, parentOldId);
                            var copiedChildren = await CopyAndReparent(connection, children, parentNewId);
                            for (int i = 0; i < copiedChildren.Count; i++) {
                                id = copiedChildren[i].Id!.Value;
                                var oldId = children[i].Id!.Value;
                                q.Enqueue((oldId, id));
                            }
                        }
                    }
                    else {
                        throw new Exception("Entry not exists or already contain entry with same name");
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
    }
    public bool Contain(long entryId, long toFolderId) {
        var connection = this._syncDb.Reader;
        return Contain(connection, entryId, toFolderId);
    }
    public async Task<bool> UpdateExtendedData(long entryId, Dictionary<string, string> extendedData) {
        var json = JsonSerializer.Serialize(extendedData);
        using(var db = await this._db.GetWriter()) {
            var _ = await db.Connection.UpdateAsync("""
                UPDATE EntryEntity
                SET ExtendedData = ?
                WHERE Id = ?
                """, json, entryId);
            return true;
        }
    }
    public async Task<bool> UpdateEmeddedIcon(long entryId, ReadOnlyMemory<byte>? embeddedIcon) {
        using(var db = await this._db.GetWriter()) {
            var _ = await db.Connection.UpdateAsync("""
                UPDATE EntryEntity
                SET EmbeddedIcon = ?
                WHERE Id = ?
                """, embeddedIcon, entryId);
            return true;
        }
    }
    //public async Task<long> GetTrashCanOrCreateIfNotExists() {
    //    using (var db = await this._db.GetReader()) {
    //        var rows = await db.Connection.SelectAsync<long>("""
    //            SELECT Id FROM EntryEntity
    //            WHERE ParentId = -1
    //                AND Name = ?
    //            """, Config.TrashCanName);
    //        foreach(var row in rows) {
    //            return row.Item1;
    //        }
    //    }
    //    var newEntry = await this.CreateFolder(-1, Config.TrashCanName);
    //    if (newEntry != null) {
    //        return newEntry.Id!.Value;
    //    } else {
    //        throw new Exception();
    //    }
    //}
    //public async Task<bool> MoveToTrashCan(List<long> entryIds, long trashCanId, IProgress<MovingProgress> progress) {
    //    using (var db = await this._db.GetWriter()) {
    //        var connection = db.Connection;
    //        await connection.BeginTransactionAsync();
    //        try {
    //            var now = DateTime.UtcNow.Ticks.ToString();
    //            foreach (var entryId in entryIds) {
    //                if (await GetEntryAsync(connection, entryId) is EntryEntity entry) {
    //                    var extendedDataJson = entry.ExtendedData;
    //                    Dictionary<string, string> extendedData;
    //                    if (extendedDataJson.Trim() == "") {
    //                        extendedData = [];
    //                    } else {
    //                        extendedData = JsonSerializer.Deserialize<Dictionary<string, string>>(extendedDataJson)!;
    //                    }
    //                    var pathSegments = GetPath(connection, entry.ParentId);
    //                    extendedData["OriginalFolder"] = JsonSerializer.Serialize(pathSegments);
    //                    extendedData["DeletedTime"] = now;
    //                    extendedDataJson = JsonSerializer.Serialize(extendedData);
    //                    var _ = await connection.UpdateAsync("""
    //                        UPDATE EntryEntity
    //                        SET ParentId = ?,
    //                            ExtendedData = ?
    //                        WHERE Id = ?
    //                        """, trashCanId, extendedDataJson, entryId);
    //                }
    //                else {
    //                    throw new Exception("Entry not exists");
    //                }
    //            }
    //            await connection.CommitTransactionAsync();
    //            return true;
    //        }
    //        catch {
    //            await connection.RollbackTransactionAsync();
    //            return false;
    //        }
    //    }
    //}
    //public static EntryEntity ClearTrashMetadata(EntryEntity entry) {
    //    var extendedDataJson = entry.ExtendedData;
    //    Dictionary<string, string> extendedData;
    //    if (extendedDataJson.Trim() == "") {
    //        extendedData = [];
    //    }
    //    else {
    //        extendedData = JsonSerializer.Deserialize<Dictionary<string, string>>(extendedDataJson)!;
    //    }
    //    if (extendedData.Remove("OriginalFolder")
    //        || extendedData.Remove("DeletedTime")) {
    //        extendedDataJson = JsonSerializer.Serialize(extendedData);
    //        entry = entry.WithExtendedData(extendedDataJson);
    //    }
    //    return entry;
    //}
    //public async Task<bool> RestoreFromTrashCan(List<long> entryIds, IProgress<MovingProgress> progress) {
    //    using (var db = await this._db.GetWriter()) {
    //        var connection = db.Connection;
    //        await connection.BeginTransactionAsync();
    //        try {
    //            var now = DateTime.UtcNow.Ticks.ToString();
    //            foreach (var entryId in entryIds) {
    //                if (await GetEntryAsync(connection, entryId) is EntryEntity entry) {
    //                    var extendedDataJson = entry.ExtendedData;
    //                    Dictionary<string, string> extendedData = JsonSerializer.Deserialize<Dictionary<string, string>>(extendedDataJson)!;
    //                    var originalFolderSegments = JsonSerializer.Deserialize<List<string>>(extendedData["OriginalFoler"])!;
    //                    var originalParentId = await GetParentIdByPath(connection, originalFolderSegments);
    //                    if (originalParentId != null) {
    //                        extendedData.Remove("OriginalFolder");
    //                        extendedData.Remove("DeletedTime");
    //                        extendedDataJson = JsonSerializer.Serialize(extendedData);
    //                        var _ = await connection.UpdateAsync("""
    //                            UPDATE EntryEntity
    //                            SET ParentId = ?,
    //                                ExtendedData = ?
    //                            WHERE Id = ?
    //                        """, originalParentId.Value, extendedDataJson, entryId);
    //                    }
    //                    else {
    //                        throw new Exception("Destination folder does not exists");
    //                    }
    //                }
    //                else {
    //                    throw new Exception("Entry not exists");
    //                }
    //            }
    //            await connection.CommitTransactionAsync();
    //            return true;
    //        }
    //        catch {
    //            await connection.RollbackTransactionAsync();
    //            return false;
    //        }
    //    }
    //}
}
