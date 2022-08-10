﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using Kudu.Contracts.Tracing;
using Kudu.Core.Tracing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileProviders.Physical;
using NuGet;

namespace Kudu.Core.Infrastructure
{
    public static class ZipArchiveExtensions
    {
        public static void AddDirectory(this ZipArchive zipArchive, string directoryPath, ITracer tracer, string directoryNameInArchive = "")
        {
            var directoryInfo = new PhysicalDirectoryInfo(new DirectoryInfo(directoryPath));
            zipArchive.AddDirectory((IDirectoryInfo) directoryInfo, tracer, directoryNameInArchive);
        }

        public static void AddDirectory(this ZipArchive zipArchive, IDirectoryInfo directory, ITracer tracer, string directoryNameInArchive, out IList<ZipArchiveEntry> files)
        {
            files = new List<ZipArchiveEntry>();
            InternalAddDirectory(zipArchive, directory, tracer, directoryNameInArchive, files);
        }

        public static void AddDirectory(this ZipArchive zipArchive, IDirectoryInfo directory, ITracer tracer, string directoryNameInArchive)
        {
            InternalAddDirectory(zipArchive, directory, tracer, directoryNameInArchive);
        }

        private static void InternalAddDirectory(ZipArchive zipArchive, IDirectoryInfo directory, ITracer tracer, string directoryNameInArchive, IList<ZipArchiveEntry> files = null)
        {
            bool any = false;
            foreach (var info in directory.GetFileSystemInfos())
            {
                any = true;
                var subDirectoryInfo = info as IDirectoryInfo;
                if (subDirectoryInfo != null)
                {
                    string childName = ForwardSlashCombine(directoryNameInArchive, subDirectoryInfo.Name);
                    InternalAddDirectory(zipArchive, subDirectoryInfo, tracer, childName, files);
                }
                else
                {
                    var entry = zipArchive.AddFile((IFileInfo)info, tracer, directoryNameInArchive);
                    files?.Add(entry);
                }
            }

            if (!any)
            {
                // If the directory did not have any files or folders, add a entry for it
                zipArchive.CreateEntry(EnsureTrailingSlash(directoryNameInArchive));
            }
        }

        private static string ForwardSlashCombine(string part1, string part2)
        {
            return Path.Combine(part1, part2).Replace('\\', '/');
        }

        public static ZipArchiveEntry AddFile(this ZipArchive zipArchive, string filePath, ITracer tracer, string directoryNameInArchive = "")
        {
            var fileInfo = new PhysicalFileInfo(new FileInfo(filePath));
            return zipArchive.AddFile((IFileInfo) fileInfo, tracer, directoryNameInArchive);
        }

        public static ZipArchiveEntry AddFile(this ZipArchive zipArchive, IFileInfo file, ITracer tracer, string directoryNameInArchive)
        {
            Stream fileStream = null;
            try
            {
                fileStream = file.OpenRead();
            }
            catch (Exception ex)
            {
                // tolerate if file in use.
                // for simplicity, any exception.
                tracer.TraceError(String.Format("{0}, {1}", file.FullName, ex));
                return null;
            }

            try
            {
                string fileName = ForwardSlashCombine(directoryNameInArchive, file.Name);
                ZipArchiveEntry entry = zipArchive.CreateEntry(fileName, CompressionLevel.Fastest);
                entry.LastWriteTime = file.LastWriteTime;

                using (Stream zipStream = entry.Open())
                {
                    fileStream.CopyTo(zipStream);
                }
                return entry;
            }
            finally
            {
                fileStream.Dispose();
            }
        }

        public static ZipArchiveEntry AddFile(this ZipArchive zip, string fileName, string fileContent)
        {
            ZipArchiveEntry entry = zip.CreateEntry(fileName, CompressionLevel.Fastest);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(fileContent);
            }
            return entry;
        }

        public static void Extract(this ZipArchive archive, string directoryName, ITracer tracer, bool doNotPreserveFileTime = false)
        {
            const int MaxExtractTraces = 5;

            var entries = archive.Entries;
            var total = entries.Count;
            var traces = 0;
            using (tracer.Step(string.Format("Extracting {0} entries to {1} directory", entries.Count, directoryName)))
            {
                foreach (ZipArchiveEntry entry in entries)
                {
                    string path = Path.Combine(directoryName, entry.FullName);
                    if (entry.Length == 0 && (path.EndsWith("/", StringComparison.Ordinal) || path.EndsWith("\\", StringComparison.Ordinal)))
                    {
                        // Extract directory
                        FileSystemHelpers.CreateDirectory(path);
                    }
                    else
                    {
                        IFileInfo fileInfo = FileSystemHelpers.FileInfoFromFileName(path);

                        string message = null;
                        // tracing first/last N files
                        if (traces < MaxExtractTraces || traces >= total - MaxExtractTraces)
                        {
                            message = string.Format("Extracting to {0} ...", fileInfo.FullName);
                        }
                        else if (traces == MaxExtractTraces)
                        {
                            message = "Omitting extracting file traces ...";
                        }

                        ++traces;

                        using (!string.IsNullOrEmpty(message) ? tracer.Step(message) : null)
                        {
                            if (!fileInfo.Directory.Exists)
                            {
                                fileInfo.Directory.Create();
                            }

                            using (Stream zipStream = entry.Open(),
                                          fileStream = fileInfo.Open(FileMode.Create, FileAccess.Write))
                            {
                                zipStream.CopyTo(fileStream);
                            }

                            // this is to allow, the file time to always be newer than destination
                            // the outcome is always replacing the destination files.
                            // it is non-optimized but workaround NPM reset file time issue (https://github.com/projectkudu/kudu/issues/2917).
                            if (!doNotPreserveFileTime)
                            {
                                fileInfo.LastWriteTimeUtc = entry.LastWriteTime.ToUniversalTime().DateTime;
                            }
                        }
                    }
                }
            }
        }

        private static string EnsureTrailingSlash(string input)
        {
            return input.EndsWith("/", StringComparison.Ordinal) ? input : input + "/";
        }
    }
}