using AzurArchive.Data.Database.Entities;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class FolderRepository {
    internal static bool CheckSubFolderExists(SQLiteReadConnection connection, long parentId, string folderName) {
        return connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FolderEntity
                WHERE ParentId = ? AND LOWER(Name) = LOWER(?)
            )
            """, parentId, folderName).First().Item1;
    }
    internal static bool CheckFolderExists(SQLiteWriteConnection connection, long folderId) {
        return folderId == -1 || connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM FolderEntity
                WHERE Id = ?
            )
            """, folderId).First().Item1;
    }
    internal static bool IsChildren(SQLiteReadConnection connection, long childrenId, long parentId) {
        long currentId = childrenId;
        while (currentId != -1) {
            if (currentId == parentId) {
                return true;
            }
            else {
                var checkRows = connection.Select<long>(
                    "SELECT ParentId FROM FolderEntity WHERE Id = ?", currentId).ToList();
                if (checkRows.Count > 0) {
                    currentId = checkRows[0].Item1;
                }
                else {
                    return false;
                }
            }
        }
        // Exit only when parentId = -1
        return false;
    }
    internal static long? GetFolderParentId(SQLiteReadConnection reader, long folderId) {
        var rows = reader.Select<long>($"""
                SELECT ParentId FROM FolderEntity WHERE Id = ?
                """, folderId).ToList();
        if (rows.Count == 0) {
            return null;
        }
        else {
            return rows[0].Item1;
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
    internal static FolderEntity? InsertFolderWithReturnId(SQLiteWriteConnection connection, long parentId, string folderName) {
        var now = DateTime.Now;
        FolderEntity rootFolder = new((-1, parentId, folderName, now, now));
        List<object[]> ids = connection.Insert([rootFolder], true);
        if (ids.Count > 0) {
            long id = (long)ids[0][0];
            rootFolder = rootFolder.WithId(id);
            return rootFolder;
        }
        else {
            return null;
        }
    }
    private static string GetPlaceholder(int count) {
        return string.Join(",", Enumerable.Repeat("?", count));
    }
}
