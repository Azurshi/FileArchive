using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal class FileRepository {
    private readonly DatabaseContextAsync _db;
    public FileRepository(DataManager manager) {
        this._db = manager.Database;
    }
    internal static async Task<List<FileEntity>> GetFiles(SQLiteReadConnection connection, long folderId) {
        var rows = await connection.SelectAsync<
            long, long, string, DateTime, DateTime, long
            >("SELECT * FROM FileEntity WHERE FolderId = ?", folderId);
        List<FileEntity> result = [];
        foreach (var row in rows) {
            result.Add(new(row));
        }
        return result;
    }
    public async Task<List<FileEntity>> GetFiles(long folderId) {
        using(var db = await _db.GetReader()) {
            return await GetFiles(db.Connection, folderId);
        }
    }
    internal static async Task<FileEntity?> GetFile(SQLiteReadConnection connection, long fileId) {
        var rows = await connection.SelectAsync<
            long, long, string, DateTime, DateTime, long
            >("SELECT * FROM FileEntity WHERE Id = ?", fileId);
        if (rows.Count > 0) {
            return new(rows[0]);
        }
        return null;
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
                "SELECT Name, FolderId FROM FolderEntity WHERE Id = ?", fileId);
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
            var rows = await connection.SelectAsync<long>(
                "SELECT Id FROM FolderEntity WHERE Id = ?", toFolderId);
            if (rows.Count == 0 || toFolderId != -1) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            var nameRows = await connection.SelectAsync<string>(
                "SELECT Name FROM FileEntity WHERE FolderId = ?", toFolderId);
            var file = await GetFile(connection, fileId);
            if (file == null || nameRows.Select(r => r.Item1).Contains(file.Name)) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            var rc = await connection.UpdateAsync(
                "UPDATE FileEntity SET FolderId = ? WHERE Id = ?", toFolderId, fileId);
            progress.Report(new(0, "Moved", true));
            return true;
        }
    }
    public async Task<bool> Copy(long fileId, long toFolderId, IProgress<MovingProgress> progress) {
        using (var db = await _db.GetWriter()) {
            var connection = db.Connection;
            var rows = await connection.SelectAsync<long>(
                "SELECT Id FROM FolderEntity WHERE Id = ?", toFolderId);
            if (rows.Count == 0 || toFolderId != -1) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
            var nameRows = await connection.SelectAsync<string>(
                "SELECT Name FROM FileEntity WHERE FolderId = ?", toFolderId);
            var file = await GetFile(connection, fileId);
            if (file == null || nameRows.Select(r => r.Item1).Contains(file.Name)) {
                progress.Report(new(-1, "Failed", true));
                return false;
            }
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
                foreach (var row in chunkRows) {
                    chunks.Add(new((newFileId, row.Item1, row.Item2)));
                }
                await connection.InsertAsync(chunks, false);
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
    }
}
