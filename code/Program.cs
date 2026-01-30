using DSync.Models;
using DSync.Utilities;
using System;
using System.IO;

namespace DSync
{
    internal class Program
    {
        private const int FailureExitCode = 1;

        private const int SuccessExitCode = 0;

        private const string Version = "3.5.0";

        private static int Main(string[] args)
        {
            try
            {
                string sourceDirectoryPath = null;
                string destinationDirectoryPath = null;
                DiffMode diffMode = DiffMode.Full;
                bool purgeDestinationDirectory = false;
                string excludedRelativeFilePaths = null;
                string excludedRelativeDirectoryPaths = null;
                bool whatIf = false;
                bool trace = false;

                CommandLine.SetSynopsis("Synchronizes the content of two directories.");

                CommandLine.SetVersion(Version);

                CommandLine.SetSyntax("/SourceDirectoryPath=<String> /DestinationDirectoryPath=<String> [/DiffMode=<String>] [/DiffCulture=<String>] [/PurgeDestinationDirectory] [/ExcludedRelativeFilePaths=<String>] [/ExcludedRelativeDirectoryPaths=<String>] [/WhatIf] [/Trace]");

                CommandLine.AddParameter("/SourceDirectoryPath", "The source directory path.", () => true, (value) => sourceDirectoryPath = NormalizeDirectoryPath(value));
                CommandLine.AddParameter("/DestinationDirectoryPath", "The destination directory path.", () => true, (value) => destinationDirectoryPath = NormalizeDirectoryPath(value));
                CommandLine.AddParameter("/DiffMode", "The mode used for detecting the differences between the source and the destination directory. Available values are: Fast, Full.", () => true, (value) => diffMode = (DiffMode)Enum.Parse(typeof(DiffMode), value));
                CommandLine.AddParameter("/PurgeDestinationDirectory", "Remove extra files and directories from the destination directory.", () => false, (value) => purgeDestinationDirectory = true);
                CommandLine.AddParameter("/ExcludedRelativeFilePaths", "A list of pipe-separated relative file paths to exclude from synchronization.", () => false, (value) => excludedRelativeFilePaths = value);
                CommandLine.AddParameter("/ExcludedRelativeDirectoryPaths", "A list of pipe-separated relative directory paths to exclude from synchronization.", () => false, (value) => excludedRelativeDirectoryPaths = value);
                CommandLine.AddParameter("/WhatIf", "Show what would have been done, without doing it.", () => false, (value) => whatIf = true);
                CommandLine.AddParameter("/Trace", "Show a more verbose log about what's going on.", () => false, (value) => trace = true);

                CommandLine.SetExamples("\"/SourceDirectoryPath=C:\\Source\" \"/DestinationDirectoryPath=C:\\Destination\" /DiffMode=Full /PurgeDestinationDirectory", "\"/SourceDirectoryPath=C:\\Source\" \"/DestinationDirectoryPath=C:\\Destination\" /DiffMode=Fast /PurgeDestinationDirectory /WhatIf");

                var parseResult = CommandLine.ParseArguments(args);

                switch (parseResult.Status)
                {
                    case CommandLine.ParseStatus.Valid:

                        Console.WriteLine("Current time is \"{0}\".", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                        Console.WriteLine("Computer name is \"{0}\".", Environment.MachineName);
                        Console.WriteLine("Operating system version is \"{0}\".", Environment.OSVersion);
                        Console.WriteLine("Operating system is {0}.", Environment.Is64BitOperatingSystem ? "64 bit" : "32 bit");
                        Console.WriteLine("Common Language Runtime version is \"{0}\".", Environment.Version);
                        Console.WriteLine("DSync version is \"{0}\".", Version);
                        Console.WriteLine("Process is {0}.", Environment.Is64BitProcess ? "64 bit" : "32 bit");
                        Console.WriteLine("Current user is \"{0}\\{1}\".", Environment.UserDomainName, Environment.UserName);

                        Console.WriteLine("Source directory path is \"{0}\".", sourceDirectoryPath);
                        Console.WriteLine("Destination directory path is \"{0}\".", destinationDirectoryPath);
                        Console.WriteLine("Diff mode is {0}.", diffMode);
                        Console.WriteLine("Purge destination directory flag is {0}.", purgeDestinationDirectory);
                        Console.WriteLine("Excluded relative file paths are \"{0}\".", excludedRelativeFilePaths);
                        Console.WriteLine("Excluded relative directory paths are \"{0}\".", excludedRelativeDirectoryPaths);
                        Console.WriteLine("What if flag is {0}.", whatIf);
                        Console.WriteLine("Trace flag is {0}.", trace);

                        SyncEngine.Sync(sourceDirectoryPath, destinationDirectoryPath, diffMode, purgeDestinationDirectory, !string.IsNullOrEmpty(excludedRelativeFilePaths) && (excludedRelativeFilePaths != "-") ? excludedRelativeFilePaths.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : null, !string.IsNullOrEmpty(excludedRelativeDirectoryPaths) && (excludedRelativeDirectoryPaths != "-") ? excludedRelativeDirectoryPaths.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : null, whatIf, trace);

                        return SuccessExitCode;

                    case CommandLine.ParseStatus.Invalid:

                        CommandLine.ShowParseErrorsInConsole(parseResult);

                        return FailureExitCode;

                    case CommandLine.ParseStatus.HelpRequested:

                        CommandLine.ShowHelpInConsole();

                        return SuccessExitCode;
                }
            }
            catch (AggregateException aggregateEx)
            {
                aggregateEx.Handle
                (
                    ex =>
                    {
                        int exDepth = 1; do
                        {
                            Console.WriteLine("{0} [{1}:{2}@{3}]\n{4}", ex.Message, exDepth, ex.GetType(), ex.Source, ex.StackTrace);

                            ex = ex.InnerException;

                            exDepth++;
                        }
                        while (ex != null); return true;
                    }
                );
            }
            catch (Exception ex)
            {
                int exDepth = 1; do
                {
                    Console.WriteLine("{0} [{1}:{2}@{3}]\n{4}", ex.Message, exDepth, ex.GetType(), ex.Source, ex.StackTrace);

                    ex = ex.InnerException;

                    exDepth++;
                }
                while (ex != null);
            }

            return FailureExitCode;
        }

        private static string NormalizeDirectoryPath(string path)
        {
            var normalizedPath = Path.GetFullPath(path); return normalizedPath.EndsWith(@"\") ? normalizedPath.Substring(0, normalizedPath.Length - 1) : normalizedPath;
        }
    }
}