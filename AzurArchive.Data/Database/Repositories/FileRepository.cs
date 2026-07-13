using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class FileRepository {
    private readonly DatabaseContextAsync _db;
    public FileRepository(DataManager manager) {
        this._db = manager.Database;
    }
    public async Task<List<FileEntity>> GetFiles(long folderId) {
        using(var db = await _db.GetReader()) {
            return await GetFiles(db.Connection, folderId);
        }
    }
    public async Task<FileEntity?> GetFile(long fileId) {
        using (var db = await _db.GetReader()) {
            return await GetFile(db.Connection, fileId);
        }
    }
    public async Task<bool> RenameFile(long fileId, string name) {
        using(var db = await _db.GetWriter()) {
            var rc = await db.Connection.ExecuteAsync(
                "UPDATE FileEntity SET Name = ? WHERE Id = ?", name, fileId);
            return true;
        }
    }
    public async Task<List<string>> GetPath(long fileId) {
        List<string> result = [];
        using (var db = await _db.GetReader()) {

            var connection = db.Connection;
            var fileRows = await connection.SelectAsync<string, long>(
                "SELECT Name, FolderId FROM FileEntity WHERE Id = ?", fileId);
            long folderId;
            if (fileRows.Count > 0) {
                result.Add(fileRows[0].Item1);
                folderId = fileRows[0].Item2;
            } else {
                return result;
            }
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
    public async Task<bool> Move(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await _db.GetWriter()) {
            var connection = db.Connection;
            if ((toFolderId == -1 || FolderRepository.CheckFolderExists(connection, toFolderId)) // To folder must exists or is root
                && (await GetFile(connection, fileId)) is FileEntity file // File must exists
                && !CheckSubFileExists(connection, toFolderId, file.Name) // Check file and folder name
                && !FolderRepository.CheckSubFolderExists(connection, toFolderId, file.Name)) {
                await connection.UpdateAsync("UPDATE FileEntity SET FolderId = ? WHERE Id = ?", toFolderId, fileId);
                progress.Report(new(0, "Moved", true));
                return true;
            } else {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
        }
    }
    public async Task<bool> Copy(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await _db.GetWriter()) {
            var connection = db.Connection;
            if ((toFolderId == -1 || FolderRepository.CheckFolderExists(connection, toFolderId))
                && CheckFileExists(connection, fileId)  
                && (await GetFile(connection, fileId)) is FileEntity file
                && file.FolderId != toFolderId
                && !FolderRepository.CheckSubFolderExists(connection, toFolderId, file.Name)
                && !CheckSubFileExists(connection, toFolderId, file.Name)) {
                await connection.BeginTransactionAsync();
                try {
                    file = file.WithIdAndFolder(-1, toFolderId);
                    var idObjs = await connection.InsertAsync([file], true);
                    if (idObjs.Count > 0) {
                        file = file.WithId((long)idObjs[0][0]);
                    }
                    else {
                        await connection.RollbackTransactionAsync();
                        progress.Report(new(-1, "Failed", true));
                        return false;
                    }
                    var chunkRows = await connection.SelectAsync<int, Hash256>($"""
                    SELECT OrderIndex, Hash FROM FileChunkRelation
                    WHERE FileId = ?
                    """, fileId);
                    long newFileId = file!.Id!.Value;
                    long oldFileId = fileId;
                    List<FileChunkRelation> chunks = [];
                    Dictionary<Hash256, int> hashIncrease = [];
                    foreach (var row in chunkRows) {
                        var hash = row.Item2;
                        chunks.Add(new((newFileId, row.Item1, hash)));
                        if (hashIncrease.TryGetValue(hash, out var increase)) {
                            hashIncrease[hash] = increase + 1;
                        } else {
                            hashIncrease[hash] = 1;
                        }
                    }
                    await connection.InsertAsync(chunks, false);
                    foreach (var (hash, increase) in hashIncrease) {
                        connection.Update("UPDATE ChunkEntity SET ReferenceCount = ReferenceCount + ? WHERE Hash = ?", increase, hash);
                    }
                    await connection.CommitTransactionAsync();
                    progress.Report(new(0, "Moved", true));
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
}
