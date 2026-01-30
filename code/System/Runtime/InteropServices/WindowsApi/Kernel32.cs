using System.IO;

namespace System.Runtime.InteropServices.WindowsApi
{
    public static class Kernel32
    {
        private const string ModuleName = "Kernel32";

        [DllImport(ModuleName, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteFile(string lpFileName);

        [DllImport(ModuleName, SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool MoveFile(string lpExistingFileName, string lpNewFileName);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool RemoveDirectory(string lpPathName);

        [DllImport(ModuleName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetFileAttributes(string lpFileName, FileAttributes dwFileAttributes);

        [DllImport(ModuleName, SetLastError = true)]
        public static extern bool SetFileTime(IntPtr hFile, ref ComTypes.FILETIME lpCreationTime, ref ComTypes.FILETIME lpLastAccessTime, ref ComTypes.FILETIME lpLastWriteTime);

        [BestFitMapping(false)]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;

            public uint ftCreationTimeLo;

            public uint ftCreationTimeHi;

            public uint ftLastAccessTimeLo;

            public uint ftLastAccessTimeHi;

            public uint ftLastWriteTimeLo;

            public uint ftLastWriteTimeHi;

            public uint nFileSizeHi;

            public uint nFileSizeLo;

            public uint dwReserved0;

            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternate;
        }
    }
}