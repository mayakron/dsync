using DSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsApi;
using System.Security.Cryptography;

namespace DSync.Utilities
{
    internal static class IOEngine
    {
        public static void CopyFile(string sourceFilePath, string destinationFilePath, bool failIfExists)
        {
            if (!Kernel32.CopyFile(sourceFilePath, destinationFilePath, failIfExists))
            {
                throw new UnauthorizedAccessException("Unable to copy file from \"" + sourceFilePath + "\" to \"" + destinationFilePath + "\".");
            }
        }

        public static void CreateDirectory(string directoryPath)
        {
            if (!Kernel32.CreateDirectory(directoryPath, IntPtr.Zero))
            {
                throw new UnauthorizedAccessException("Unable to create directory \"" + directoryPath + "\".");
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            if (!Kernel32.RemoveDirectory(directoryPath))
            {
                throw new UnauthorizedAccessException("Unable to delete directory \"" + directoryPath + "\".");
            }
        }

        public static void DeleteFile(string filePath)
        {
            if (!Kernel32.DeleteFile(filePath))
            {
                throw new UnauthorizedAccessException("Unable to delete file \"" + filePath + "\".");
            }
        }

        public static byte[] GetFileHash(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var hashEngine = MD5.Create())
                {
                    return hashEngine.ComputeHash(fileStream);
                }
            }
        }

        public static void RenameDirectoryInTwoPhases(string oldDirectoryPath, string newParentPath, string newDirectoryName)
        {
            var newDirectoryPath = newParentPath + @"\" + newDirectoryName;

            string tmpDirectoryPath;

            do { tmpDirectoryPath = newParentPath + @"\" + Guid.NewGuid().ToString("N"); } while (Directory.Exists(tmpDirectoryPath));

            if (!Kernel32.MoveFile(oldDirectoryPath, tmpDirectoryPath))
            {
                throw new UnauthorizedAccessException("Unable to rename directory from \"" + newDirectoryPath + "\" to \"" + tmpDirectoryPath + "\".");
            }

            if (!Kernel32.MoveFile(tmpDirectoryPath, newDirectoryPath))
            {
                throw new UnauthorizedAccessException("Unable to rename directory from \"" + tmpDirectoryPath + "\" to \"" + newDirectoryPath + "\".");
            }
        }

        public static void RenameFileInTwoPhases(string oldFilePath, string newParentPath, string newFileName)
        {
            var newFilePath = newParentPath + @"\" + newFileName;

            string tmpFilePath;

            do { tmpFilePath = newParentPath + @"\" + Guid.NewGuid().ToString("N"); } while (File.Exists(tmpFilePath));

            if (!Kernel32.MoveFile(oldFilePath, tmpFilePath))
            {
                throw new UnauthorizedAccessException("Unable to rename file from \"" + newFilePath + "\" to \"" + tmpFilePath + "\".");
            }

            if (!Kernel32.MoveFile(tmpFilePath, newFilePath))
            {
                throw new UnauthorizedAccessException("Unable to rename file from \"" + tmpFilePath + "\" to \"" + newFilePath + "\".");
            }
        }

        public static void Scan(string path, List<FileSystemEntry> fileEntries, List<FileSystemEntry> directoryEntries, string[] excludedRelativeFilePaths, string[] excludedRelativeDirectoryPaths)
        {
            var substringIndex = path.Length + 1;

            DirectoryInfoUtility.WindowsApiTraverseFileSystemEntries
            (
                path,
                "*.*",
                false,
                (findPath, findData) =>
                {
                    var absolutePath = findPath + @"\" + findData.cFileName;
                    var relativePath = absolutePath.Substring(substringIndex);

                    if (!IsExcludedRelativeFilePath(relativePath, excludedRelativeFilePaths))
                    {
                        fileEntries.Add
                        (
                            new FileSystemEntry
                            {
                                ParentPath = findPath,
                                Name = findData.cFileName,
                                AbsolutePath = absolutePath,
                                RelativePath = relativePath,
                                Attributes = findData.dwFileAttributes,
                                LastWriteTime = (ulong)findData.ftLastWriteTimeLo + ((ulong)findData.ftLastWriteTimeHi << 32),
                                Size = (ulong)findData.nFileSizeLo + ((ulong)findData.nFileSizeHi << 32)
                            }
                        );
                    }
                },
                (findPath, findData) =>
                {
                    var absolutePath = findPath + "\\" + findData.cFileName;
                    var relativePath = absolutePath.Substring(substringIndex);

                    if (!IsExcludedRelativeDirectoryPath(relativePath, excludedRelativeDirectoryPaths))
                    {
                        directoryEntries.Add
                        (
                            new FileSystemEntry
                            {
                                ParentPath = findPath,
                                Name = findData.cFileName,
                                AbsolutePath = absolutePath,
                                RelativePath = relativePath,
                                Attributes = findData.dwFileAttributes
                            }
                        );

                        return true;
                    }

                    return false;
                }
            );
        }

        public static void SetFileOrDirectoryAttributes(string fileOrDirectoryPath, FileAttributes attributes)
        {
            if (!Kernel32.SetFileAttributes(fileOrDirectoryPath, attributes))
            {
                throw new UnauthorizedAccessException("Unable to set attributes of file or directory \"" + fileOrDirectoryPath + "\".");
            }
        }

        public static void SetFileOrDirectoryAttributes(string fileOrDirectoryPath, FileAttributes attributes, FileAttributes significantAttributesMask)
        {
            if (!Kernel32.SetFileAttributes(fileOrDirectoryPath, attributes & significantAttributesMask))
            {
                throw new UnauthorizedAccessException("Unable to set attributes of file or directory \"" + fileOrDirectoryPath + "\".");
            }
        }

        public static void SetFileOrDirectoryLastWriteTime(string fileOrDirectoryPath, ulong lastWriteTime)
        {
            SetFileOrDirectoryLastWriteTime(fileOrDirectoryPath, new System.Runtime.InteropServices.ComTypes.FILETIME { dwLowDateTime = (int)(lastWriteTime & 4294967295UL), dwHighDateTime = (int)(lastWriteTime >> 32) });
        }

        public static void SetFileOrDirectoryLastWriteTime(string fileOrDirectoryPath, System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime)
        {
            var fileHandle = Kernel32.CreateFile(fileOrDirectoryPath, 256, 0, IntPtr.Zero, 3, 128, IntPtr.Zero);

            if (fileHandle.ToInt64() == -1L)
            {
                throw new UnauthorizedAccessException("Unable to set last write time of file or directory \"" + fileOrDirectoryPath + "\".");
            }

            try
            {
                var creationTime = new System.Runtime.InteropServices.ComTypes.FILETIME { dwLowDateTime = 0, dwHighDateTime = 0 };
                var lastAccessTime = new System.Runtime.InteropServices.ComTypes.FILETIME { dwLowDateTime = 0, dwHighDateTime = 0 };

                if (!Kernel32.SetFileTime(fileHandle, ref creationTime, ref lastAccessTime, ref lastWriteTime))
                {
                    throw new UnauthorizedAccessException("Unable to set last write time of file or directory \"" + fileOrDirectoryPath + "\".");
                }
            }
            finally
            {
                Kernel32.CloseHandle(fileHandle);
            }
        }

        private static bool IsExcludedRelativeDirectoryPath(string relativeDirectoryPath, string[] excludedRelativeDirectoryPaths)
        {
            return (excludedRelativeDirectoryPaths != null) && excludedRelativeDirectoryPaths.Any(excludedRelativeDirectoryPath => (StringComparer.OrdinalIgnoreCase.Compare(relativeDirectoryPath, excludedRelativeDirectoryPath) == 0) || ((relativeDirectoryPath.Length > excludedRelativeDirectoryPath.Length) && (StringComparer.OrdinalIgnoreCase.Compare(relativeDirectoryPath.Substring(0, excludedRelativeDirectoryPath.Length + 1), excludedRelativeDirectoryPath + @"\") == 0)));
        }

        private static bool IsExcludedRelativeFilePath(string relativeFilePath, string[] excludedRelativeFilePaths)
        {
            return (excludedRelativeFilePaths != null) && excludedRelativeFilePaths.Any(excludedRelativeFilePath => StringComparer.OrdinalIgnoreCase.Compare(relativeFilePath, excludedRelativeFilePath) == 0);
        }
    }
}