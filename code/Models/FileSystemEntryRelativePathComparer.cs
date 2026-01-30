using System;
using System.Collections.Generic;
using System.Globalization;

namespace DSync.Models
{
    internal class FileSystemEntryRelativePathComparer : IComparer<FileSystemEntry>
    {
        private readonly StringComparer comparer;

        public FileSystemEntryRelativePathComparer()
        {
            this.comparer = StringComparer.OrdinalIgnoreCase;
        }

        public int Compare(FileSystemEntry x, FileSystemEntry y)
        {
            return this.comparer.Compare(x.RelativePath, y.RelativePath);
        }
    }
}