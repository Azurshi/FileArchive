namespace AzurArchive.Data;

using AzurArchive.Data.Database.Repositories;
using AzurArchive.Data.ServiceImplements;
using AzurArchive.Data.ServiceImplemments;
using AzurArchive.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using SQLiteORM;
using SQLiteORM.Internal;
using SQLitePCL;
using System;
using System.Diagnostics;

public static class ServiceCollectionExtensions {
    public static IServiceCollection RegisterData(this IServiceCollection services) {
        // Global
        services.AddSingleton<DataManager>();

        // Database
        services.AddTransient<ChunkContentReader>();
        services.AddTransient<ChunkContentWriter>();
        services.AddSingleton<ChunkRepository>();
        services.AddSingleton<FileRepository>();
        services.AddSingleton<FolderRepository>();
        services.AddSingleton<ArchiveRepository>();

        // Data
        services.AddSingleton<IArchiver, Archiver>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IFolderService, FolderService>();
        services.AddSingleton<IAppSetting, AppSetting>();
        var result = TypeMapExtend.Register(
            typeof(Hash256),
            SQLiteKeyword.Blob,
            Hash256Binder,
            Hash256Reader
            );
        result &= TypeMapExtend.Register(
            typeof(Memory<byte>),
            SQLiteKeyword.Blob,
            MemoryBinder,
            MemoryReader
            );
        if (result == false) {
            Debug.WriteLine("Failed to register database field");
        }
        return services;
    }
    private static int Hash256Binder(sqlite3_stmt stmt, int index, object value) {
        Hash256 hash = (Hash256)value;
        return raw.sqlite3_bind_blob(stmt, index, hash.AsReadOnlySpan());
    }
    private static object Hash256Reader(sqlite3_stmt stmt, int index) {
        ReadOnlySpan<byte> span = raw.sqlite3_column_blob(stmt, index);
        Hash256 hash = new();
        span.CopyTo(hash.AsSpan());
        return hash;
    }
    private static readonly Memory<byte> MemoryBuffer = new byte[Config.MaxChunkSize];
    private static int MemoryBinder(sqlite3_stmt stmt, int index, object value) {
        Memory<byte> memory = (Memory<byte>)value;
        return raw.sqlite3_bind_blob(stmt, index, memory.Span);
    }
    private static object MemoryReader(sqlite3_stmt stmt, int index) {
        ReadOnlySpan<byte> span = raw.sqlite3_column_blob(stmt, index);
        span.CopyTo(MemoryBuffer.Span);
        return MemoryBuffer[..span.Length];
    }
}