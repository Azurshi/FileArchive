using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal class FolderRepository {
    private readonly DatabaseContextAsync _db;
    private readonly DatabaseContext _syncDb;
    public FolderRepository(DataManager manager) {
        this._db = manager.Database;
        this._syncDb = manager.SyncDatabase;
    }
    public async Task<FolderEntity?> CreateFolder(long parentId, string folderName) {
        using(var db = await _db.GetWriter()) {
            var rows = await db.Connection.SelectAsync<string>(
                "SELECT Name FROM FolderEntity WHERE ParentId = ?", parentId);
            bool exists = false;
            foreach(var row in rows) {
                if (row.Item1.Equals(folderName)) {
                    exists = true;
                    break;
                }
            }
            if (exists) {
                return null;
            }
            var now = DateTime.Now;
            FolderEntity entity = new((-1, parentId, folderName, now, now));
            List<object[]> ids = await db.Connection.InsertAsync([entity], true);
            if (ids.Count > 0) {
                long id = (long)ids[0][0];
                return entity.WithId(id);
            } else {
                return null;
            }
        }
    }
    internal async Task<FolderEntity?> GetFolder(SQLiteReadConnection connection, long folderId) {
        var rows = await connection.SelectAsync<
            long, long, string, DateTime, DateTime
            >("SELECT * FROM FolderEntity WHERE Id = ?", folderId);
        if (rows.Count > 0) {
            return new(rows[0]);
        }
        else {
            return null;
        }
    }
    public async Task<FolderEntity?> GetFolder(long folderId) {
        using (var db = await _db.GetReader()) {
            return await GetFolder(db.Connection, folderId);   
        }
    }
    internal static async Task<List<FolderEntity>> GetFolders(SQLiteReadConnection connection, long parentId) {
        var rows = await connection.SelectAsync<
            long, long, string, DateTime, DateTime
            >("SELECT * FROM FolderEntity WHERE ParentId = ?", parentId);
        List<FolderEntity> result = new(rows.Count);
        foreach (var row in rows) {
            result.Add(new(row));
        }
        return result;
    }
    public async Task<List<FolderEntity>> GetFolders(long parentId) {
        using(var db = await _db.GetReader()) {
            return await GetFolders(db.Connection, parentId);
        }
    }
    public async Task<bool> RenameFolder(long folderId, string name) {
        using(var db = await _db.GetWriter()) {
            var rc = await db.Connection.ExecuteAsync(
                "UPDATE FolderEntity SET Name = ? WHERE Id = ?", name, folderId);
            return true;
        }
    }
    public async Task<List<string>> GetPath(long folderId) {
        List<string> result = [];
        if (folderId == -1) {
            return result;
        }
        using(var db = await _db.GetReader()) {
            var connection = db.Connection;
            while (folderId != -1) {
                var rows = await connection.SelectAsync<string, long>(
                    "SELECT Name, ParentId FROM FolderEntity WHERE Id = ?", folderId);
                if (rows.Count > 0) {
                    folderId = rows[0].Item2;
                    result.Insert(0, rows[0].Item1);
                }
            }
        }
        return result;
    }
    public async Task<bool> Move(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        using(var db = await _db.GetWriter()) {
            var connection = db.Connection;
            var rows = await connection.SelectAsync<long>(
                "SELECT Id FROM FolderEntity WHERE Id IN (?,?)", folderId,  toFolderId);
            if (rows.Count == 0 || (rows.Count == 1 && toFolderId != -1)) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            var nameRows = await connection.SelectAsync<string>(
                "SELECT Name FROM FolderEntity WHERE ParentId = ?", toFolderId);
            var folder = await GetFolder(connection, folderId);
            if (folder == null || nameRows.Select(r => r.Item1).Contains(folder.Name)) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            var rc = await connection.UpdateAsync(
                "UPDATE FolderEntity SET ParentId = ? WHERE Id = ?", toFolderId, folderId);
            progress.Report(new(0, "Moved", true));
            return true;
        }
    }
    public async Task<bool> Copy(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        using(var db = await _db.GetWriter()) {
            var connection = db.Connection;
            var rows = await connection.SelectAsync<long>(
                "SELECT Id FROM FolderEntity WHERE Id IN (?,?)", folderId, toFolderId);
            if (rows.Count == 0 || (rows.Count == 1 && toFolderId != -1)) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            var nameRows = await connection.SelectAsync<string>(
                "SELECT Name FROM FolderEntity WHERE ParentId = ?", toFolderId);
            var folder = await GetFolder(connection, folderId);
            if (folder == null || nameRows.Select(r => r.Item1).Contains(folder.Name)) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            rows = await connection.SelectAsync<long>(
                "SELECT ParentId FROM FolderEntity WHERE Id = ?", folderId);
            if (rows.Count > 0 && rows[0].Item1 ==toFolderId) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            await connection.BeginTransactionAsync();
            try {
                Queue<(long fromId, long toId)> folderQ = [];
                folderQ.Enqueue((folderId, toFolderId));
                var now = DateTime.Now;
                while (folderQ.Count > 0) {
                    progress.Report(new(folderQ.Count, "Moving", false));
                    (folderId, toFolderId) = folderQ.Dequeue();
                    // Handle folders
                    var folders = await GetFolders(connection, folderId);
                    List<long> originalIds = [];
                    for(int i=0; i<folders.Count; i++) {
                        originalIds.Add(folders[i].Id!.Value);
                        // Reset Id and set parent id
                        folders[i] = folders[i].WithIdAndParent(-1, toFolderId);
                    }
                    var idObjs = await connection.InsertAsync(folders, true);
                    for (int i = 0; i < folders.Count; i++) {
                        long returnId = (long)idObjs[i][0];
                        folderQ.Enqueue((originalIds[i], returnId));
                    }
                    // Handle files
                    var files = await FileRepository.GetFiles(connection, folderId);
                    originalIds.Clear();
                    Dictionary<long, long> fileIdMapping = [];
                    for(int i=0; i<files.Count; i++) {
                        originalIds.Add(files[i].Id!.Value);
                        // Reset Id and set folder id
                        files[i] = files[i].WithIdAndFolder(-1, toFolderId);
                    }
                    idObjs  = await connection.InsertAsync(files, true);
                    for(int i=0; i<files.Count;i++) {
                        long returnId = (long)idObjs[i][0];
                        files[i] = files[i].WithId(returnId);
                        fileIdMapping[originalIds[i]] = returnId;
                    }
                    // Handle chunk
                    var chunkRows = await connection.SelectAsync<long, int, Hash256>($"""
                        SELECT * FROM FileChunkRelation
                        WHERE FileId IN ({GetPlaceholder(files.Count)})
                        """, files.Select(f => (object)f.Id!.Value).ToArray());
                    List<FileChunkRelation> chunks = [];
                    foreach (var row in chunkRows) {
                        long oldFileId = row.Item1;
                        long newFileId = fileIdMapping[oldFileId];
                        chunks.Add(new((newFileId, row.Item2, row.Item3)));
                    }
                    await connection.InsertAsync(chunks, false);
                }
                await connection.CommitTransactionAsync();
                progress.Report(new(0, "Completed", true));
                return true;
            }
            catch {
                await connection.RollbackTransactionAsync();
                progress.Report(new(-1, "Failed", true));
                return false;
            }
        }
    }
    public bool ContainFolder(long folderId, long childrenId) {
        var rows = _syncDb.Reader.Select<long>("SELECT Id FROM FolderEntity WHERE Id = ? AND ParentId = ?", childrenId, folderId);
        foreach(var _ in rows) {
            return true;
        }
        return false;
    }
    public bool ContainFile(long folderId, long childrenId) {
        var rows = _syncDb.Reader.Select<long>("SELECT Id FROM FileEntity WHERE Id = ? AND FolderId = ?", childrenId, folderId);
        foreach (var _ in rows) {
            return true;
        }
        return false;
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}
