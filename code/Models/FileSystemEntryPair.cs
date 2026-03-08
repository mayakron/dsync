using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DSync.Models
{
    internal class FileSystemEntryPair
    {
        public readonly FileSystemEntry DestinationFileSystemEntry;

        public readonly FileSystemEntry SourceFileSystemEntry;

        public bool ShouldUpdateAttributes;

        public bool ShouldUpdateContent;

        public bool ShouldUpdateName;

        public FileSystemEntryPair(FileSystemEntry sourceFileSystemEntry, FileSystemEntry destinationFileSystemEntry)
        {
            this.SourceFileSystemEntry = sourceFileSystemEntry;
            this.DestinationFileSystemEntry = destinationFileSystemEntry;
        }

        public static bool AttributesAreDifferent(FileSystemEntry a, FileSystemEntry b, FileAttributes significantAttributesMask)
        {
            return (a.Attributes & significantAttributesMask) != (b.Attributes & significantAttributesMask);
        }

        public static bool HashIsDifferent(FileSystemEntry a, FileSystemEntry b)
        {
            return !a.Hash.SequenceEqual(b.Hash);
        }

        public static bool LastWriteTimeIsDifferent(FileSystemEntry a, FileSystemEntry b)
        {
            const ulong TimeStampGranularityInTicks = 20000000; // 2 seconds.

            return MathUtility.Distance(a.LastWriteTime, b.LastWriteTime) > TimeStampGranularityInTicks;
        }

        public static bool NameIsDifferent(FileSystemEntry a, FileSystemEntry b)
        {
            return StringComparer.Ordinal.Compare(a.Name, b.Name) != 0;
        }

        public static bool SizeIsDifferent(FileSystemEntry a, FileSystemEntry b)
        {
            return a.Size != b.Size;
        }
    }
}