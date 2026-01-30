using DSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DSync.Utilities
{
    internal static class SyncEngine
    {
        public static void Sync(string sourceDirectoryPath, string destinationDirectoryPath, DiffMode diffMode, bool purgeDestinationDirectory, string[] excludedRelativeFilePaths, string[] excludedRelativeDirectoryPaths, bool whatIf, bool trace)
        {
            // The below attributes mask determines what attributes are considered significant for synchronization. When using the CopyFile function, attributes are copied to the new file.

            const FileAttributes significantAttributesMask = FileAttributes.Directory | FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.Normal | FileAttributes.ReadOnly | FileAttributes.System;

            // When the source and destination directories reside on different physical disks, parallelism might improve performance.

            var parallelIOSuggested = ParallelIOSuggested(sourceDirectoryPath, destinationDirectoryPath);

            // Comparisons between file names and paths must take the culture into consideration.

            var fileSystemEntryRelativePathComparer = new FileSystemEntryRelativePathComparer();

            // Sync started.

            Console.WriteLine("Sync started...");

            // Scanning directories.

            Console.WriteLine("Scanning directories...");

            List<FileSystemEntry> sourceFiles = null;
            List<FileSystemEntry> sourceDirectories = null;

            List<FileSystemEntry> destinationFiles = null;
            List<FileSystemEntry> destinationDirectories = null;

            if (parallelIOSuggested)
            {
                Parallel.Invoke
                (
                    () =>
                    {
                        sourceFiles = new List<FileSystemEntry>();
                        sourceDirectories = new List<FileSystemEntry>();

                        IOEngine.Scan(sourceDirectoryPath, sourceFiles, sourceDirectories, excludedRelativeFilePaths, excludedRelativeDirectoryPaths);

                        sourceFiles.Sort(fileSystemEntryRelativePathComparer);
                        sourceDirectories.Sort(fileSystemEntryRelativePathComparer);
                    },
                    () =>
                    {
                        destinationFiles = new List<FileSystemEntry>();
                        destinationDirectories = new List<FileSystemEntry>();

                        IOEngine.Scan(destinationDirectoryPath, destinationFiles, destinationDirectories, excludedRelativeFilePaths, excludedRelativeDirectoryPaths);

                        destinationFiles.Sort(fileSystemEntryRelativePathComparer);
                        destinationDirectories.Sort(fileSystemEntryRelativePathComparer);
                    }
                );
            }
            else
            {
                sourceFiles = new List<FileSystemEntry>();
                sourceDirectories = new List<FileSystemEntry>();

                IOEngine.Scan(sourceDirectoryPath, sourceFiles, sourceDirectories, excludedRelativeFilePaths, excludedRelativeDirectoryPaths);

                sourceFiles.Sort(fileSystemEntryRelativePathComparer);
                sourceDirectories.Sort(fileSystemEntryRelativePathComparer);

                destinationFiles = new List<FileSystemEntry>();
                destinationDirectories = new List<FileSystemEntry>();

                IOEngine.Scan(destinationDirectoryPath, destinationFiles, destinationDirectories, excludedRelativeFilePaths, excludedRelativeDirectoryPaths);

                destinationFiles.Sort(fileSystemEntryRelativePathComparer);
                destinationDirectories.Sort(fileSystemEntryRelativePathComparer);
            }

            // Build a series of "diff" lists for later synchronization.

            Console.WriteLine("Comparing directories...");

            var directoriesToCreate = new List<FileSystemEntry>();
            var directoriesToDelete = new List<FileSystemEntry>();
            var directoriesToUpdate = new List<FileSystemEntryPair>();

            var sourceDirectoryIndex = 0;
            var destinationDirectoryIndex = 0;

            while ((sourceDirectoryIndex < sourceDirectories.Count) && (destinationDirectoryIndex < destinationDirectories.Count))
            {
                var sourceDirectory = sourceDirectories[sourceDirectoryIndex];
                var destinationDirectory = destinationDirectories[destinationDirectoryIndex];

                var compareResult = fileSystemEntryRelativePathComparer.Compare(sourceDirectory, destinationDirectory);

                if (compareResult < 0)
                {
                    directoriesToCreate.Add(sourceDirectory);

                    sourceDirectoryIndex++;
                }
                else if (compareResult > 0)
                {
                    directoriesToDelete.Add(destinationDirectory);

                    destinationDirectoryIndex++;
                }
                else
                {
                    FileSystemEntryPair currentItem = null;

                    if (FileSystemEntryPair.NameIsDifferent(sourceDirectory, destinationDirectory))
                    {
                        if (currentItem == null)
                        {
                            currentItem = new FileSystemEntryPair(sourceDirectory, destinationDirectory);
                        }

                        currentItem.ShouldUpdateNameCasing = true;
                    }

                    if (FileSystemEntryPair.AttributesAreDifferent(sourceDirectory, destinationDirectory, significantAttributesMask))
                    {
                        if (currentItem == null)
                        {
                            currentItem = new FileSystemEntryPair(sourceDirectory, destinationDirectory);
                        }

                        currentItem.ShouldUpdateAttributes = true;
                    }

                    if (currentItem != null)
                    {
                        directoriesToUpdate.Add(currentItem);
                    }

                    sourceDirectoryIndex++;
                    destinationDirectoryIndex++;
                }
            }

            while (sourceDirectoryIndex < sourceDirectories.Count)
            {
                directoriesToCreate.Add(sourceDirectories[sourceDirectoryIndex]);

                sourceDirectoryIndex++;
            }

            while (destinationDirectoryIndex < destinationDirectories.Count)
            {
                directoriesToDelete.Add(destinationDirectories[destinationDirectoryIndex]);

                destinationDirectoryIndex++;
            }

            var filesToDelete = new List<FileSystemEntry>();
            var filesToCreate = new List<FileSystemEntry>();
            var filesToUpdate = new List<FileSystemEntryPair>();
            var filesToVerify = new List<FileSystemEntryPair>();

            var sourceFileIndex = 0;
            var destinationFileIndex = 0;

            switch (diffMode)
            {
                case DiffMode.Fast:

                    while ((sourceFileIndex < sourceFiles.Count) && (destinationFileIndex < destinationFiles.Count))
                    {
                        var sourceFile = sourceFiles[sourceFileIndex];
                        var destinationFile = destinationFiles[destinationFileIndex];

                        var compareResult = fileSystemEntryRelativePathComparer.Compare(sourceFile, destinationFile);

                        if (compareResult < 0)
                        {
                            filesToCreate.Add(sourceFile);

                            sourceFileIndex++;
                        }
                        else if (compareResult > 0)
                        {
                            filesToDelete.Add(destinationFile);

                            destinationFileIndex++;
                        }
                        else
                        {
                            FileSystemEntryPair currentItem = null;

                            if (FileSystemEntryPair.NameIsDifferent(sourceFile, destinationFile))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateNameCasing = true;
                            }

                            if (FileSystemEntryPair.AttributesAreDifferent(sourceFile, destinationFile, significantAttributesMask))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateAttributes = true;
                            }

                            if (FileSystemEntryPair.LastWriteTimeIsDifferent(sourceFile, destinationFile))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateLastWriteTime = true;
                            }

                            if (FileSystemEntryPair.SizeIsDifferent(sourceFile, destinationFile))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateContent = true;
                            }

                            if (currentItem != null)
                            {
                                filesToUpdate.Add(currentItem);
                            }

                            sourceFileIndex++;
                            destinationFileIndex++;
                        }
                    }

                    break;

                case DiffMode.Full:

                    while ((sourceFileIndex < sourceFiles.Count) && (destinationFileIndex < destinationFiles.Count))
                    {
                        var sourceFile = sourceFiles[sourceFileIndex];
                        var destinationFile = destinationFiles[destinationFileIndex];

                        var compareResult = fileSystemEntryRelativePathComparer.Compare(sourceFile, destinationFile);

                        if (compareResult < 0)
                        {
                            filesToCreate.Add(sourceFile);

                            sourceFileIndex++;
                        }
                        else if (compareResult > 0)
                        {
                            filesToDelete.Add(destinationFile);

                            destinationFileIndex++;
                        }
                        else
                        {
                            FileSystemEntryPair currentItem = null;

                            if (FileSystemEntryPair.NameIsDifferent(sourceFile, destinationFile))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateNameCasing = true;
                            }

                            if (FileSystemEntryPair.AttributesAreDifferent(sourceFile, destinationFile, significantAttributesMask))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateAttributes = true;
                            }

                            if (FileSystemEntryPair.LastWriteTimeIsDifferent(sourceFile, destinationFile))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateLastWriteTime = true;
                            }

                            if (FileSystemEntryPair.SizeIsDifferent(sourceFile, destinationFile))
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                currentItem.ShouldUpdateContent = true;

                                filesToUpdate.Add(currentItem);
                            }
                            else
                            {
                                if (currentItem == null)
                                {
                                    currentItem = new FileSystemEntryPair(sourceFile, destinationFile);
                                }

                                filesToVerify.Add(currentItem);
                            }

                            sourceFileIndex++;
                            destinationFileIndex++;
                        }
                    }

                    break;
            }

            while (sourceFileIndex < sourceFiles.Count)
            {
                filesToCreate.Add(sourceFiles[sourceFileIndex]);

                sourceFileIndex++;
            }

            while (destinationFileIndex < destinationFiles.Count)
            {
                filesToDelete.Add(destinationFiles[destinationFileIndex]);

                destinationFileIndex++;
            }

            if (filesToVerify.Count > 0)
            {
                if (parallelIOSuggested)
                {
                    Parallel.Invoke
                    (
                        () =>
                        {
                            for (int i = 0; i < filesToVerify.Count; i++)
                            {
                                var currentItem = filesToVerify[i];

                                currentItem.SourceFileSystemEntry.Hash = IOEngine.GetFileHash(currentItem.SourceFileSystemEntry.AbsolutePath);
                            }
                        },
                        () =>
                        {
                            for (int i = 0; i < filesToVerify.Count; i++)
                            {
                                var currentItem = filesToVerify[i];

                                currentItem.DestinationFileSystemEntry.Hash = IOEngine.GetFileHash(currentItem.DestinationFileSystemEntry.AbsolutePath);
                            }
                        }
                    );
                }
                else
                {
                    for (int i = 0; i < filesToVerify.Count; i++)
                    {
                        var currentItem = filesToVerify[i];

                        currentItem.SourceFileSystemEntry.Hash = IOEngine.GetFileHash(currentItem.SourceFileSystemEntry.AbsolutePath);

                        currentItem.DestinationFileSystemEntry.Hash = IOEngine.GetFileHash(currentItem.DestinationFileSystemEntry.AbsolutePath);
                    }
                }

                for (int i = 0; i < filesToVerify.Count; i++)
                {
                    var currentItem = filesToVerify[i];

                    if (FileSystemEntryPair.HashIsDifferent(currentItem.SourceFileSystemEntry, currentItem.DestinationFileSystemEntry))
                    {
                        currentItem.ShouldUpdateContent = true;

                        filesToUpdate.Add(currentItem);
                    }
                    else if (currentItem.ShouldUpdateNameCasing || currentItem.ShouldUpdateAttributes || currentItem.ShouldUpdateLastWriteTime)
                    {
                        filesToUpdate.Add(currentItem);
                    }
                }
            }

            // If tracing is active, print detailed info about scan results.

            if (trace)
            {
                foreach (var item in sourceDirectories)
                {
                    Console.WriteLine($"SD|{item.RelativePath}|{(uint)item.Attributes}");
                }

                foreach (var item in sourceFiles)
                {
                    Console.WriteLine($"SF|{item.RelativePath}|{(uint)item.Attributes}|{item.LastWriteTime}|{item.Size}|{((item.Hash != null) && (item.Hash.Length > 0) ? BitConverter.ToString(item.Hash) : string.Empty)}");
                }

                foreach (var item in destinationDirectories)
                {
                    Console.WriteLine($"DD|{item.RelativePath}|{(uint)item.Attributes}");
                }

                foreach (var item in destinationFiles)
                {
                    Console.WriteLine($"DF|{item.RelativePath}|{(uint)item.Attributes}|{item.LastWriteTime}|{item.Size}|{((item.Hash != null) && (item.Hash.Length > 0) ? BitConverter.ToString(item.Hash) : string.Empty)}");
                }
            }

            // Delete files from the destination directory.

            if (purgeDestinationDirectory)
            {
                if (filesToDelete.Count > 0)
                {
                    for (int i = filesToDelete.Count - 1; i >= 0; i--)
                    {
                        var currentItem = filesToDelete[i];

                        Console.WriteLine("Deleting file \"{0}\"...", currentItem.AbsolutePath);

                        if (!whatIf)
                        {
                            if (currentItem.Attributes.HasFlag(FileAttributes.ReadOnly))
                            {
                                IOEngine.SetFileOrDirectoryAttributes(currentItem.AbsolutePath, FileAttributes.Normal);
                            }

                            IOEngine.DeleteFile(currentItem.AbsolutePath);
                        }
                    }
                }
            }

            // Delete directories from the destination directory.

            if (purgeDestinationDirectory)
            {
                if (directoriesToDelete.Count > 0)
                {
                    for (int i = directoriesToDelete.Count - 1; i >= 0; i--)
                    {
                        var currentItem = directoriesToDelete[i];

                        Console.WriteLine("Deleting directory \"{0}\"...", currentItem.AbsolutePath);

                        if (!whatIf)
                        {
                            IOEngine.DeleteDirectory(currentItem.AbsolutePath);
                        }
                    }
                }
            }

            // Create directories in the destination directory.

            if (directoriesToCreate.Count > 0)
            {
                for (int i = 0; i < directoriesToCreate.Count; i++)
                {
                    var currentItem = directoriesToCreate[i];

                    var destinationAbsoluteDirectoryPath = destinationDirectoryPath + @"\" + currentItem.RelativePath;

                    Console.WriteLine("Creating directory \"{0}\"...", destinationAbsoluteDirectoryPath);

                    if (!whatIf)
                    {
                        IOEngine.CreateDirectory(destinationAbsoluteDirectoryPath);
                        IOEngine.SetFileOrDirectoryAttributes(destinationAbsoluteDirectoryPath, currentItem.Attributes, significantAttributesMask);
                    }
                }
            }

            // Update directories in the destination directory.

            if (directoriesToUpdate.Count > 0)
            {
                for (int i = 0; i < directoriesToUpdate.Count; i++)
                {
                    var currentItem = directoriesToUpdate[i];

                    Console.WriteLine("Updating directory \"{0}\"...", currentItem.DestinationFileSystemEntry.AbsolutePath);

                    if (!whatIf)
                    {
                        if (currentItem.ShouldUpdateNameCasing)
                        {
                            IOEngine.RenameDirectoryInTwoPhases(currentItem.DestinationFileSystemEntry.AbsolutePath, currentItem.DestinationFileSystemEntry.ParentPath, currentItem.SourceFileSystemEntry.Name);
                        }

                        if (currentItem.ShouldUpdateAttributes)
                        {
                            IOEngine.SetFileOrDirectoryAttributes(currentItem.DestinationFileSystemEntry.AbsolutePath, currentItem.SourceFileSystemEntry.Attributes, significantAttributesMask);
                        }
                    }
                }
            }

            // Create files in the destination directory.

            if (filesToCreate.Count > 0)
            {
                for (int i = 0; i < filesToCreate.Count; i++)
                {
                    var currentItem = filesToCreate[i];

                    var destinationAbsoluteFilePath = destinationDirectoryPath + @"\" + currentItem.RelativePath;

                    Console.WriteLine("Creating file \"{0}\"...", destinationAbsoluteFilePath);

                    if (!whatIf)
                    {
                        // I am assuming here that CopyFile also copies attributes.

                        IOEngine.CopyFile(currentItem.AbsolutePath, destinationAbsoluteFilePath, true);
                        IOEngine.SetFileOrDirectoryLastWriteTime(destinationAbsoluteFilePath, currentItem.LastWriteTime);
                    }
                }
            }

            // Update files in the destination directory.

            if (filesToUpdate.Count > 0)
            {
                for (int i = 0; i < filesToUpdate.Count; i++)
                {
                    var currentItem = filesToUpdate[i];

                    Console.WriteLine("Updating file \"{0}\"...", currentItem.DestinationFileSystemEntry.AbsolutePath);

                    if (!whatIf)
                    {
                        if (currentItem.ShouldUpdateContent)
                        {
                            if (currentItem.DestinationFileSystemEntry.Attributes.HasFlag(FileAttributes.ReadOnly))
                            {
                                IOEngine.SetFileOrDirectoryAttributes(currentItem.DestinationFileSystemEntry.AbsolutePath, FileAttributes.Normal, significantAttributesMask);
                            }

                            // I am assuming here that CopyFile also copies attributes.

                            IOEngine.DeleteFile(currentItem.DestinationFileSystemEntry.AbsolutePath);
                            IOEngine.CopyFile(currentItem.SourceFileSystemEntry.AbsolutePath, currentItem.DestinationFileSystemEntry.AbsolutePath, true);
                            IOEngine.SetFileOrDirectoryLastWriteTime(currentItem.DestinationFileSystemEntry.AbsolutePath, currentItem.SourceFileSystemEntry.LastWriteTime);
                        }
                        else
                        {
                            if (currentItem.ShouldUpdateNameCasing)
                            {
                                IOEngine.RenameFileInTwoPhases(currentItem.DestinationFileSystemEntry.AbsolutePath, currentItem.DestinationFileSystemEntry.ParentPath, currentItem.SourceFileSystemEntry.Name);
                            }

                            if (currentItem.ShouldUpdateAttributes)
                            {
                                IOEngine.SetFileOrDirectoryAttributes(currentItem.DestinationFileSystemEntry.AbsolutePath, currentItem.SourceFileSystemEntry.Attributes, significantAttributesMask);
                            }

                            if (currentItem.ShouldUpdateLastWriteTime)
                            {
                                IOEngine.SetFileOrDirectoryLastWriteTime(currentItem.DestinationFileSystemEntry.AbsolutePath, currentItem.SourceFileSystemEntry.LastWriteTime);
                            }
                        }
                    }
                }
            }

            // Sync completed.

            Console.WriteLine($"Sync completed using {(parallelIOSuggested ? "parallel" : "sequential")} I/O.");
        }

        private static bool ParallelIOSuggested(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            var sourceRootPath = Path.GetPathRoot(Path.GetFullPath(sourceDirectoryPath));
            var destinationRootPath = Path.GetPathRoot(Path.GetFullPath(destinationDirectoryPath));

            return !string.Equals(sourceRootPath, destinationRootPath, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}