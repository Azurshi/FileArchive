using AzurArchive.Data.Database.Entities;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class FileRepository {
    internal static bool CheckSubFileExists(SQLiteReadConnection connection, long folderId, string fileName) {
        return connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FileEntity
                WHERE FolderId = ? AND LOWER(Name) = LOWER(?)
            )
            """, folderId, fileName).First().Item1;
    }
    internal static bool CheckFileExists(SQLiteWriteConnection connection, long fileId) {
        return fileId == -1 || connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FileEntity
                WHERE Id = ?
            )
            """, fileId).First().Item1;
    }
    internal static long? GetFileParentId(SQLiteReadConnection reader, long folderId) {
        var row = reader.Select<long>($"""
                SELECT FolderId FROM FileEntity WHERE Id = ?
                """, folderId).ToList();
        if (row.Count == 0) {
            return null;
        }
        else {
            return row[0].Item1;
        }
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
    internal static async Task<FileEntity?> GetFile(SQLiteReadConnection connection, long fileId) {
        var rows = await connection.SelectAsync<
            long, long, string, DateTime, DateTime, long
            >("SELECT * FROM FileEntity WHERE Id = ?", fileId);
        if (rows.Count > 0) {
            return new(rows[0]);
        }
        return null;
    }
    internal static FileEntity? InsertFileWithReturnId(SQLiteWriteConnection connection, long folderId, string filePath) {
        var now = DateTime.Now;
        FileInfo file = new(filePath);
        FileEntity fileEntity = new((-1, folderId, file.Name, now, now, file.Length));
        List<object[]> ids = connection.Insert([fileEntity], true);
        if (ids.Count > 0) {
            long id = (long)ids[0][0];
            fileEntity = fileEntity.WithId(id);
            return fileEntity;
        }
        else {
            return null;
        }
    }
}
