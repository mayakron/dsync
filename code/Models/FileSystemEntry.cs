using System.IO;

namespace DSync.Models
{
    internal class FileSystemEntry
    {
        public string AbsolutePath;

        public FileAttributes Attributes;

        public byte[] Hash;

        public ulong LastWriteTime;

        public string Name;

        public string ParentPath;

        public string RelativePath;

        public ulong Size;
    }
}