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
            Console.WriteLine("Querying files...");
            List<string> files = new List<string>();
            string[] extensions = options.FileExtensions.ToArray();

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
                catch (Exception e) when (e is UnauthorizedAccessException || e is PathTooLongException)
                {
                    // Just ignore and carry on
                    ConsoleHelper.WriteTaggedMessage(MessageType.Error, $"Skipped {path}");
                }
            }

            // Execute the query and check the results
            ExploreDirectory(options.SourceDirectoryCurrent ? Directory.GetCurrentDirectory() : options.SourceDirectory);
            if (files.Count < 2)
            {
                statistics.StopTracking();
                Console.WriteLine("No files were found in the source directory");
                return statistics;
            }

            // Initialize the mapping between each target file and its MD5 hash
            Console.Write($"Preprocessing {files.Count} files... ");
            ConcurrentDictionary<string, List<string>> map = new ConcurrentDictionary<string, List<string>>();
            Console.ForegroundColor = ConsoleColor.Gray;
            using (AsciiProgressBar progressBar = new AsciiProgressBar())
            {
                int i = 0;
                HashSet<string> exclusions = new HashSet<string>(options.FileExclusions.Select(entry => $".{entry}"));
                Parallel.ForEach(files, file =>
                {
                    // Compute the MD5 hash
                    if (exclusions.Count > 0 && exclusions.Contains(Path.GetExtension(file))) return;
                    try
                    {
                        using (FileStream stream = File.OpenRead(file))
                        using (MD5 md5 = MD5.Create())
                        {
                            byte[] hash = md5.ComputeHash(stream);
                            string hex = BitConverter.ToString(hash);

                            // Get the actual key for the current file
                            string key;
                            switch (options.Match)
                            {
                                case MatchMode.MD5AndExtension: key = $"{hex}|{Path.GetExtension(file)}"; break;
                                case MatchMode.MD5AndFilename: key = $"{hex}|{Path.GetFileName(file)}"; break;
                                default: key = hex; break;
                            }

                            // Update the mapping
                            map.AddOrUpdate(key, new List<string> { file }, (_, list) =>
                            {
                                list.Add(file);
                                return list;
                            });
                            progressBar.Report((double)Interlocked.Increment(ref i) / files.Count);
                        }
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        // Just ignore
                    }
                });
            }

            // Process each duplicate file that has been found
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{Environment.NewLine}Processing duplicates... ");
            Console.ForegroundColor = ConsoleColor.Gray;
            using (AsciiProgressBar progressBar = new AsciiProgressBar())
            {
                int i = 0, count = map.Values.Count;
                Parallel.ForEach(map, pair =>
                {
                    if (pair.Value.Count < 2) return;
                    statistics.AddDuplicates(pair.Value);
                    (long ticks, string path) = (File.GetCreationTimeUtc(pair.Value[0]).Ticks, pair.Value[0]);
                    foreach (string duplicate in pair.Value.Skip(1))
                    {
                        long creation = File.GetCreationTimeUtc(duplicate).Ticks;
                        if (creation >= ticks) handler?.Invoke(duplicate);
                        else
                        {
                            handler?.Invoke(path);
                            (ticks, path) = (creation, duplicate);
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
            string logfile = Path.Combine(path, $"logfile_{DateTime.Now:yyyy-mm-dd[hh-mm-ss]}.txt");
            using (StreamWriter writer = File.CreateText(logfile))
            {
                writer.WriteLine("========");
                writer.WriteLine(Parser.Default.FormatCommandLine(options));
                foreach (string line in statistics.ExtractStatistics(options.Verbose)) writer.WriteLine(line);
                writer.WriteLine("========");
                foreach(KeyValuePair<string, IReadOnlyList<string>> pair in statistics.DuplicatesMap)
                {
                    writer.WriteLine($"{Environment.NewLine}{pair.Key}");
                    foreach (string duplicate in pair.Value) writer.WriteLine(duplicate);
                }
            }
        }

        #endregion
    }
}
