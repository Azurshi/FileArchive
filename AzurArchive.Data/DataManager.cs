using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.Database.Relations;
using AzurArchive.Data.ServiceImplements;
using AzurArchive.Data.Services;
using SQLiteORM;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzurArchive.Data; 
public class DataManager {
    internal readonly DatabaseContextAsync Database;
    private DatabaseContext? _syncDatabase;
    internal DatabaseContext SyncDatabase => _syncDatabase ?? throw new NotInitializedException();
    private string _saveDirectory = string.Empty;
    public string SaveDirectory => _saveDirectory;
    public DataManager() {
        this.Database = new();
    }
    public async Task Start(string saveDirectory) {
        this._saveDirectory = saveDirectory;
        Batteries_V2.Init();
        List<Type> tables = [
            typeof(FolderEntity), typeof(FileEntity),
            typeof(ChunkEntity),

            typeof(FileChunkRelation),
            ];
        List<string> beforeCreateTable = [
            "PRAGMA auto_vacuum = INCREMENTAL;",
            "VACUUM;"
            ];
        string dbPath = Path.Join(saveDirectory, "appData.db");
        await Database.StartDatabase(dbPath, 2, tables, beforeCreateTable);
        this._syncDatabase = new(dbPath, [], []);
        string shardFolder = Path.Combine(saveDirectory, "Shards");
        Directory.CreateDirectory(shardFolder);
        List<Type> shardTables = [
            typeof(ChunkContent)
            ];
        for (int i = 0; i < Config.Shards; i++) {
            string shardPath = Path.Join(shardFolder, $"shard{i}.db");
            DatabaseContext shard = new(shardPath, shardTables, beforeCreateTable);
            shard.Dispose();
        }
    }
    public async Task Dispose() {
        Database.Dispose();
    }
}
