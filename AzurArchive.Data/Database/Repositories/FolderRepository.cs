using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class FolderRepository {
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
    public async Task<FolderEntity?> GetFolder(long folderId) {
        using (var db = await _db.GetReader()) {
            return await GetFolder(db.Connection, folderId);   
        }
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
        // Validate when move to children folder
        using(var db = await _db.GetWriter()) {
            var connection = db.Connection;
            if ((toFolderId == -1 || CheckFolderExists(connection, toFolderId)) // To folder must exists or is root
                && (await GetFolder(connection, folderId)) is FolderEntity folder // Folder must exists
                && !FileRepository.CheckSubFileExists(connection, toFolderId, folder.Name) // Check file and folder name
                && !CheckSubFolderExists(connection, toFolderId, folder.Name)
                && !IsChildren(connection, toFolderId, folderId)) {
                await connection.UpdateAsync("UPDATE FolderEntity SET ParentId = ? WHERE Id = ?", toFolderId, folderId);
                progress.Report(new(0, "Moved", true));
                return true;
            } else {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
        }
    }
    public async Task<bool> Copy(long folderId, long toFolderId, IProgress<MovingProgress> progress) {
        using(var db = await _db.GetWriter()) {
            var connection = db.Connection;
            if ((toFolderId == -1 || CheckFolderExists(connection, toFolderId))
                && CheckFolderExists(connection, folderId)
                && (await GetFolder(connection, folderId)) is FolderEntity folder
                && folder.ParentId != toFolderId
                && !CheckSubFolderExists(connection, toFolderId, folder.Name)
                && !FileRepository.CheckSubFileExists(connection, toFolderId, folder.Name)) {
                await connection.BeginTransactionAsync();
                try {
                    Queue<(long fromId, long toId)> folderQ = [];
                    // Insert root folder first
                    var rootFolder = InsertFolderWithReturnId(connection, toFolderId, folder.Name) ?? throw new Exception();
                    folderQ.Enqueue((folderId, rootFolder.Id!.Value));
                    var now = DateTime.Now;
                    while (folderQ.Count > 0) {
                        progress.Report(new(folderQ.Count, "Moving", false));
                        (folderId, toFolderId) = folderQ.Dequeue();
                        // Handle folders
                        var folders = await GetFolders(connection, folderId);
                        List<long> originalIds = [];
                        for (int i = 0; i < folders.Count; i++) {
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
                        for (int i = 0; i < files.Count; i++) {
                            originalIds.Add(files[i].Id!.Value);
                            // Reset Id and set folder id
                            files[i] = files[i].WithIdAndFolder(-1, toFolderId);
                        }
                        idObjs = await connection.InsertAsync(files, true);
                        for (int i = 0; i < files.Count; i++) {
                            long returnId = (long)idObjs[i][0];
                            files[i] = files[i].WithId(returnId);
                            fileIdMapping[originalIds[i]] = returnId;
                        }
                        // Handle chunk
                        var chunkRows = await connection.SelectAsync<long, int, Hash256>($"""
                        SELECT * FROM FileChunkRelation
                        WHERE FileId IN ({GetPlaceholder(originalIds.Count)})
                        """, originalIds.Cast<object>().ToArray());
                        List<FileChunkRelation> chunks = [];
                        Dictionary<Hash256, int> hashIncrease = [];
                        foreach (var row in chunkRows) {
                            var hash = row.Item3;
                            long oldFileId = row.Item1;
                            long newFileId = fileIdMapping[oldFileId];
                            chunks.Add(new((newFileId, row.Item2, row.Item3)));
                            if (hashIncrease.TryGetValue(hash, out var increase)) {
                                hashIncrease[hash] = increase + 1;
                            }
                            else {
                                hashIncrease[hash] = 1;
                            }
                        }
                        await connection.InsertAsync(chunks, false);
                        foreach (var (hash, increase) in hashIncrease) {
                            connection.Update("UPDATE ChunkEntity SET ReferenceCount = ReferenceCount + ? WHERE Hash = ?", increase, hash);
                        }
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
            else {
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
}
