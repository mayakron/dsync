using System.Runtime.InteropServices.WindowsApi;

namespace System.IO
{
    public class DirectoryInfoUtility
    {
        public static void WindowsApiTraverseFileSystemEntries(string path, string searchPattern, bool ignoreUnauthorizedAccessExceptions, Action<string, Kernel32.WIN32_FIND_DATA> onFiles, Func<string, Kernel32.WIN32_FIND_DATA, bool> onDirectories)
        {
            var findHandle = Kernel32.FindFirstFile(path + @"\" + searchPattern, out Kernel32.WIN32_FIND_DATA findData);

            if (findHandle.ToInt64() == -1)
            {
                if (!ignoreUnauthorizedAccessExceptions)
                {
                    throw new UnauthorizedAccessException(string.Format("Access to the path \"{0}\" is denied.", path));
                }

                return;
            }

            try
            {
                do
                {
                    if ((findData.cFileName != ".") && (findData.cFileName != ".."))
                    {
                        if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
                        {
                            if (onDirectories(path, findData))
                            {
                                WindowsApiTraverseFileSystemEntries(path + @"\" + findData.cFileName, searchPattern, ignoreUnauthorizedAccessExceptions, onFiles, onDirectories);
                            }
                        }
                        else
                        {
                            onFiles(path, findData);
                        }
                    }
                }
                while (Kernel32.FindNextFile(findHandle, out findData));
            }
            finally
            {
                Kernel32.FindClose(findHandle);
            }
        }
    }
}