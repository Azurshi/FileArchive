using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Services;
using Microsoft.UI.Xaml.Controls;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class EntryRepository {
    internal static async Task<bool> ContainAsync(SQLiteReadConnection connection, long entryId, string name) {
        var rows = await connection.SelectAsync<string>(
            "SELECT Name FROM EntryEntity WHERE ParentId = ?", entryId);
        foreach (var row in rows) {
            if (row.Item1.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }
    internal static bool Contain(SQLiteReadConnection connection, long entryId, string name) {
        var rows = connection.Select<string>(
            "SELECT Name FROM EntryEntity WHERE ParentId = ?", entryId);
        foreach (var row in rows) {
            if (row.Item1.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }
    internal static async Task<bool> Contain(SQLiteReadConnection connection, long entryId, long childrenId) {
        var rows = await connection.SelectAsync<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM EntryEntity
                WHERE Id = ?
                    AND ParentId = ?
            )
            """, childrenId, entryId);
        return rows.First().Item1;
    }
    internal static long? GetParentId(SQLiteReadConnection connection, long entryId) {
        var rows = connection.Select<long>("""
            SELECT ParentId FROM EntryEntity
            WHERE Id = ?
            """, entryId);
        foreach(var row in rows) {
            return row.Item1;
        }
        return null;
    }
    internal static async Task<EntryEntity?> GetEntry(SQLiteReadConnection connection, long entryId) {
        var rows = await connection.SelectAsync<
            long, long, int, long,
            DateTime, DateTime, DateTime, DateTime,
            string, string>("""
            SELECT * FROM EntryEntity
            WHERE Id = ?
            """, entryId);
        if (rows.Count > 0) {
            return new(rows[0]);
        } else {
            return null;
        }
    }
    internal static List<EntryEntity> GetChildren(SQLiteReadConnection connection, long entryId) {
        var rows = connection.Select<
            long, long, int, long,
            DateTime, DateTime, DateTime, DateTime,
            string, string>("""
            SELECT * FROM EntryEntity
            WHERE ParentId = ?
            """, entryId);
        List<EntryEntity> result = [];
        foreach (var row in rows) {
            result.Add(new(row));
        }
        return result;
    }
    internal static async Task<List<EntryEntity>> GetChildrenAsync(SQLiteReadConnection connection, long entryId) {
        var rows = await connection.SelectAsync<
            long, long, int, long,
            DateTime, DateTime, DateTime, DateTime,
            string, string>("""
            SELECT * FROM EntryEntity
            WHERE ParentId = ?
            """, entryId);
        List<EntryEntity> result = [];
        foreach(var row in rows) {
            result.Add(new(row));
        }
        return result;
    }
    internal static async Task<bool> CheckExistsAsync(SQLiteReadConnection connection, long entryId) {
        var rows = await connection.SelectAsync<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM EntryEntity
                WHERE Id = ?
            )
            """, entryId);
        return rows.First().Item1;
    }
    internal static bool CheckExists(SQLiteReadConnection connection, long entryId) {
        var rows = connection.Select<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM EntryEntity
                WHERE Id = ?
            )
            """, entryId);
        return rows.First().Item1;
    }
    internal static async Task<List<EntryEntity>> CopyAndReparent(SQLiteWriteConnection connection, List<EntryEntity> entries, long parentId) {
        var copiedEntries = entries.Select(c => c.WithIdAndParent(-1, parentId)).ToList();
        var ids = await connection.InsertAsync(copiedEntries, true);
        if (ids.Count != copiedEntries.Count) {
            throw new Exception("Database error");
        }
        for (int i = 0; i < copiedEntries.Count; i++) {
            var id = (long)ids[i][0];
            copiedEntries[i] = copiedEntries[i].WithId(id);
            await connection.ExecuteAsync("""
                INSERT INTO EntryContentEntity (Id, Data)
                SELECT ?, Data
                FROM EntryContentEntity
                WHERE Id = ?
                """, id, entries[0].Id!.Value);
        }
        return copiedEntries;
    }
    internal static ValueTuple<EntryEntity, List<ValueTuple<EntryEntity, string>>> InsertRecursive(SQLiteWriteConnection connection, long rootParentId, string rootPath) {
        List<ValueTuple<EntryEntity, string>> dtos = [];
        EntryEntity? rootEntry = null;
        Queue<ValueTuple<string, long>> q = [];
        q.Enqueue((rootPath, rootParentId));
        bool isRoot = true;
        while(q.Count > 0) {
            var (path, parentId) = q.Dequeue();
            // Ignore symbolic link
            if (new FileInfo(path).LinkTarget != null) {
                isRoot = false;
                continue;
            }
            bool isFile = File.Exists(path);
            if (isRoot && Contain(connection, parentId, Path.GetFileName(path))) {
                throw new Exception($"Folder already contain: {Path.GetFileName(path)}");
            }
            if (isFile) {
                FileInfo file = new(path);
                EntryEntity entry = new((
                    -1, parentId, (int)file.Attributes, file.Length,
                    file.CreationTimeUtc, file.LastWriteTimeUtc, file.LastAccessTimeUtc, file.LastWriteTimeUtc,
                    file.Name, ""));
                dtos.Add((entry, path));
            }
            else {
                DirectoryInfo folder = new(path);
                EntryEntity entry = new((
                    -1, parentId, (int)folder.Attributes, 0,
                    folder.CreationTimeUtc, folder.LastWriteTimeUtc, folder.LastAccessTimeUtc, folder.LastWriteTimeUtc,
                    folder.Name, ""));
                var idObjs = connection.Insert([entry], true);
                var id = (long)idObjs[0][0];
                foreach(var file in Directory.GetFiles(path)) {
                    q.Enqueue((file, id));
                }
                foreach(var directory in Directory.GetDirectories(path)) {
                    q.Enqueue((directory, id));
                }
                if (isRoot) {
                    rootEntry = entry.WithId(id);
                }
            }
            isRoot = false;
        }
        if (rootEntry != null) {
            var ids = connection.Insert(dtos.Select(d => d.Item1).ToList(), true).Select(obj => (long)obj[0]).ToList();
            for(int i=0; i<dtos.Count; i++) {
                var dto = dtos[i];
                dtos[i] = (dto.Item1.WithId(ids[i]), dto.Item2);
            }
            if (rootEntry.IsFolder()) {
                return (rootEntry, dtos);
            }
            else {
                rootEntry = dtos[0].Item1;
                return (rootEntry, dtos);
            }
        }
        else {
            throw new Exception();
        }
    }
}
