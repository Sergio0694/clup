using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using clup.Enums;
using clup.Options;
using clup.Options.Abstract;
using CommandLine;
using JetBrains.Annotations;

namespace clup.Core
{
    /// <summary>
    /// The core class that contains the actual logic of the clup executable
    /// </summary>
    internal static class ClupEngine
    {
        #region APIs

        /// <summary>
        /// Executes the delete command, and logs the deleted files if requested
        /// </summary>
        /// <param name="options">The command options</param>
        public static void Run(DeleteOptions options)
        {
            if (string.IsNullOrEmpty(options.LogDirectory)) Run(options, File.Delete);
            else
            {
                ClupStatisticsManager statistics = Run(options, File.Delete);
                WriteLog(options.LogDirectory, options, statistics);
            }
        }

        /// <summary>
        /// Executes the move command
        /// </summary>
        /// <param name="options">The command options</param>
        public static void Run(MoveOptions options)
        {
            Directory.CreateDirectory(options.TargetDirectory);
            void Handler(string path)
            {
                string filename = Path.GetFileName(path);
                File.Move(path, Path.Combine(options.TargetDirectory, filename));
            }

            Run(options, Handler);
        }

        /// <summary>
        /// Executes the list command
        /// </summary>
        /// <param name="options">The command options</param>
        public static void Run(ListOptions options)
        {
            // Find the duplicate files
            ClupStatisticsManager statistics = Run(options, null);

            // Write the log to disk
            if (options.LogDirectoryRoot)
            {
                WriteLog(options.SourceDirectoryCurrent ? Directory.GetCurrentDirectory() : options.SourceDirectory, options, statistics);
            }
            else if (!string.IsNullOrEmpty(options.LogDirectory))
            {
                Directory.CreateDirectory(options.LogDirectory);
                WriteLog(options.LogDirectory, options, statistics);
            }
        }

        #endregion

        #region Implementation

        // Executes the requested command
        [NotNull]
        private static ClupStatisticsManager Run([NotNull] ClupOptionsBase options, [CanBeNull] Action<string> handler)
        {
            // Stats
            ClupStatisticsManager statistics = new ClupStatisticsManager();
            Console.ForegroundColor = ConsoleColor.White;

            // Initial arguments validation
            options.Validate();

            // Prepare the files query
            ConsoleHelper.WriteLine("Querying files...");
            List<string> files = new List<string>();
            string[] extensions = options.FileExtensions.Select(ext => ext.ToLowerInvariant()).ToArray();

            // Local functions to manually explore the source directory (to be able to handle errors)
            void ExploreDirectory(string path)
            {
                try
                {
                    IEnumerable<string> query = extensions.Length == 0
                        ? Directory.EnumerateFiles(path, "*")
                        : extensions.SelectMany(extension => Directory.EnumerateFiles(path, $"*.{extension}"));
                    files.AddRange(query);
                    foreach (string subdirectory in Directory.EnumerateDirectories(path))
                        ExploreDirectory(subdirectory);
                }
                catch (Exception e) when (e is UnauthorizedAccessException || e is PathTooLongException || e is DirectoryNotFoundException)
                {
                    // Just ignore and carry on
                    if (options.Verbose) ConsoleHelper.WriteTaggedMessage(MessageType.Error, path);
                }
            }

            // Execute the query and check the results
            ExploreDirectory(options.SourceDirectoryCurrent ? Directory.GetCurrentDirectory() : options.SourceDirectory);
            if (files.Count < 2)
            {
                statistics.StopTracking();
                ConsoleHelper.WriteLine("No files were found in the source directory");
                return statistics;
            }
            ConsoleHelper.WriteTaggedMessage(MessageType.Info, $"Identified {files.Count} files");

            // Look for files that have at least another file with the same size
            ConsoleHelper.Write("Filtering files... ");
            ConcurrentDictionary<long, List<string>> sizeMap = new ConcurrentDictionary<long, List<string>>();
            Console.ForegroundColor = ConsoleColor.Gray;
            HashSet<string> exclusions = new HashSet<string>(options.FileExclusions.Select(entry => $".{entry.ToLowerInvariant()}"));
            using (AsciiProgressBar progressBar = new AsciiProgressBar())
            {
                int i = 0;
                Parallel.ForEach(files, file =>
                {
                    if (exclusions.Count > 0 && exclusions.Contains(Path.GetExtension(file).ToLowerInvariant())) return;
                    try
                    {
                        long size = new FileInfo(file).Length;
                        sizeMap.AddOrUpdate(size, new List<string> { file }, (_, list) =>
                        {
                            list.Add(file);
                            return list;
                        });
                        progressBar.Report((double)Interlocked.Increment(ref i) / files.Count);
                    }
                    catch (Exception e) when (e is IOException)
                    {
                        // Carry on
                    }
                });
            }
            string[] filtered = sizeMap.Values.Where(group => group.Count > 1).SelectMany(l => l).ToArray();
            ConsoleHelper.WriteTaggedMessage(MessageType.Info, $"Found {sizeMap.Values.Sum(l => l.Count > 1 ? l.Count - 1 : 0)} potential duplicate(s)");

            // Initialize the mapping between each target file and its MD5 hash
            Console.ForegroundColor = ConsoleColor.White;
            ConsoleHelper.Write("Preprocessing filtered files... ");
            ConcurrentDictionary<string, List<string>> hashMap = new ConcurrentDictionary<string, List<string>>();
            Console.ForegroundColor = ConsoleColor.Gray;
            using (AsciiProgressBar progressBar = new AsciiProgressBar())
            {
                int i = 0;
                Parallel.ForEach(filtered, file =>
                {
                    // Compute the MD5 hash
                    if (exclusions.Count > 0 && exclusions.Contains(Path.GetExtension(file).ToLowerInvariant())) return;
                    try
                    {
                        using (FileStream stream = File.OpenRead(file))
                        using (MD5 md5 = MD5.Create())
                        {
                            byte[] hash = md5.ComputeHash(stream);
                            string base64 = Convert.ToBase64String(hash);

                            // Get the actual key for the current file
                            string key;
                            switch (options.Hash)
                            {
                                case HashMode.MD5AndExtension: key = $"{base64}|{Path.GetExtension(file).ToLowerInvariant()}"; break;
                                case HashMode.MD5AndFilename: key = $"{base64}|{Path.GetFileName(file)}"; break;
                                default: key = base64; break;
                            }

                            // Update the mapping
                            hashMap.AddOrUpdate(key, new List<string> { file }, (_, list) =>
                            {
                                list.Add(file);
                                return list;
                            });
                            progressBar.Report((double)Interlocked.Increment(ref i) / filtered.Length);
                        }
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        // Why u do dis?
                    }
                });
            }

            // Process each duplicate file that has been found
            Console.ForegroundColor = ConsoleColor.White;
            ConsoleHelper.Write("Processing duplicates... ");
            Console.ForegroundColor = ConsoleColor.Gray;
            using (AsciiProgressBar progressBar = new AsciiProgressBar())
            {
                int i = 0, count = hashMap.Values.Count;
                Parallel.ForEach(hashMap, pair =>
                {
                    // Only keep the original file in each group
                    if (pair.Value.Count < 2) return;
                    statistics.AddDuplicates(pair.Value);
                    (long ticks, string path) = (File.GetCreationTimeUtc(pair.Value[0]).Ticks, pair.Value[0]);
                    foreach (string duplicate in pair.Value.Skip(1))
                    {
                        try
                        {
                            long creation = File.GetCreationTimeUtc(duplicate).Ticks;
                            if (creation >= ticks) handler?.Invoke(duplicate);
                            else
                            {
                                handler?.Invoke(path);
                                (ticks, path) = (creation, duplicate);
                            }
                        }
                        catch
                        {
                            // Whops!
                            if (options.Verbose) ConsoleHelper.WriteTaggedMessage(MessageType.Error, path);
                        }
                    }

                    // Update the progress bar and the statistics
                    statistics.AddDuplicates(pair.Key, path, pair.Value);
                    progressBar.Report((double)Interlocked.Increment(ref i) / count);
                });
            }

            // Display the statistics
            statistics.StopTracking();
            Console.Write(Environment.NewLine);
            foreach (string info in statistics.ExtractStatistics(options.Verbose))
            {
                ConsoleHelper.WriteTaggedMessage(MessageType.Info, info);
            }

            return statistics;
        }

        // Writes a complete log of the processed duplicates to the specified directory
        private static void WriteLog([NotNull] string path, [NotNull] ClupOptionsBase options, [NotNull] ClupStatisticsManager statistics)
        {
            ConsoleHelper.WriteLine("Writing log file...");
            string logfile = Path.Combine(path, $"logfile_{DateTime.Now:yyyy-mm-dd[hh-mm-ss]}.txt");
            using (StreamWriter writer = File.CreateText(logfile))
            {
                writer.WriteLine("========");
                writer.WriteLine(Parser.Default.FormatCommandLine(options));
                foreach (string line in statistics.ExtractStatistics(options.Verbose)) writer.WriteLine(line);
                writer.WriteLine("========");
                foreach (KeyValuePair<string, IReadOnlyList<string>> pair in statistics.DuplicatesMap)
                {
                    writer.WriteLine($"{Environment.NewLine}{pair.Key}");
                    foreach (string duplicate in pair.Value) writer.WriteLine(duplicate);
                }
            }
        }

        #endregion
    }
}
