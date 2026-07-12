using AzurArchive.Data.Database.Entities;
using AzurArchive.Data.ServiceImplemments;
using AzurArchive.Data.Services;
using SQLiteORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AzurArchive.Data.Database.Repositories;

internal partial class ArchiveRepository {
    public FolderEntity? ImportFolder(long parentId, FolderDto rootDto, int nWorkers, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            // Pre-check
            // Then create root folder
            // Then insert folder tree
            if (CheckFolderExists(writer, parentId)
                && !CheckSubFolderExists(writer, parentId, rootDto.Name)
                && !CheckSubFileExists(writer, parentId, rootDto.Name)
                && CreateRootFolder(writer, parentId, rootDto.Name) is FolderEntity rootFolder
                && InsertFileAndFolder(writer, rootDto, rootFolder.Id!.Value) is List<FileToArchive> files
                ) {
                // Process chunk
                ImportChunks(this._saveFolder, writer,Config.DatabaseName, files, nWorkers, progress, token);
                // Finalize
                writer.CommitTransaction();
                progress.Report(new(-1, -1, "Completed", true));
                return rootFolder;
            }
            else {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }
        }
        catch {
            writer.RollbackTransaction();
            progress.Report(new(-1, -1, "Failed", true));
            return null;
        }
        finally {
            writer.Dispose(false);
        }
    }
    public FileEntity? ImportFile(long folderId, string filePath, IProgress<ArchiveProgress> progress, CancellationToken token) {
        string dbPath = Path.Join(this._saveFolder, Config.DatabaseName);
        SQLiteWriteConnection writer = new(dbPath);
        writer.BeginTransaction();
        try {
            string fileName = Path.GetFileName(filePath);
            // Precheck
            // Then insert file
            if (CheckFolderExists(writer, folderId)
                && !CheckSubFolderExists(writer, folderId, fileName)
                && !CheckSubFileExists(writer, folderId, fileName)
                && InsertFile(writer, folderId, filePath) is FileEntity fileEntity
                ) {
                // Process chunk
                ImportChunks(this._saveFolder, writer, Config.DatabaseName, [new(fileEntity.Id!.Value, fileEntity.Name, filePath)], 1, progress, token);
                // Finalize
                writer.CommitTransaction();
                progress.Report(new(-1, -1, "Completed", true));
                return fileEntity;
            }
            else {
                writer.RollbackTransaction();
                progress.Report(new(-1, -1, "Failed", true));
                return null;
            }
        }
        catch {
            writer.RollbackTransaction();
            progress.Report(new(-1, -1, "Failed", true));
            return null;
        }
        finally {
            writer.Dispose(false);
        }
    }
}
