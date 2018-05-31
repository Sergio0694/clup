using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using clup.Enums;
using clup.Options;
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
                ConcurrentQueue<string> deletions = new ConcurrentQueue<string>();
                Run(options, path =>
                {
                    deletions.Enqueue(path);
                    File.Delete(path);
                });
                WriteLog(options.LogDirectory, options, deletions);
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
            ConcurrentQueue<string> duplicates = new ConcurrentQueue<string>();
            Run(options, path => duplicates.Enqueue(path));

            // Write the log to disk
            if (options.TargetRoot) WriteLog(options.SourceDirectory, options, duplicates);
            else if (!string.IsNullOrEmpty(options.TargetDirectory))
            {
                Directory.CreateDirectory(options.TargetDirectory);
                WriteLog(options.TargetDirectory, options, duplicates);
            }
        }

        #endregion

        #region Implementation

        // Executes the requested command
        private static void Run([NotNull] ClupOptionsBase options, [NotNull] Action<string> handler)
        {
            // Stats
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int processed = 0;
            long bytes = 0;
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
                        : extensions.SelectMany(extension => Directory.EnumerateFiles(options.SourceDirectory, $"*.{extension}"));
                    files.AddRange(query);
                    foreach (string subdirectory in Directory.EnumerateDirectories(path))
                        ExploreDirectory(subdirectory);
                }
                catch (Exception e) when (e is UnauthorizedAccessException || e is PathTooLongException)
                {
                    // Just ignore and carry on
                }
            }

            // Execute the query and check the results
            ExploreDirectory(options.SourceDirectory);
            if (files.Count < 2)
            {
                stopwatch.Stop();
                Console.WriteLine("No files were found in the source directory");
                return;
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
                    using (MD5 md5 = MD5.Create())
                        try
                        {
                            using (FileStream stream = File.OpenRead(file))
                            {
                                byte[] hash = md5.ComputeHash(stream);
                                string hex = BitConverter.ToString(hash);

                                // Get the actual key for the current file
                                string key;
                                switch (options.Match)
                                {
                                    case MatchMode.MD5AndExtension: key = $"{hex}{Path.GetExtension(file)}"; break;
                                    case MatchMode.MD5AndFilename: key = $"{hex}{file}"; break;
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
                Parallel.ForEach(map.Values, duplicates =>
                {
                    if (duplicates.Count < 2) return;
                    long filesize = new FileInfo(duplicates[0]).Length;
                    (long ticks, string path) = (File.GetCreationTimeUtc(duplicates[0]).Ticks, duplicates[0]);
                    foreach (string duplicate in duplicates.Skip(1))
                    {
                        long creation = File.GetCreationTimeUtc(duplicate).Ticks;
                        if (creation >= ticks) handler(duplicate);
                        else
                        {
                            handler(path);
                            (ticks, path) = (creation, duplicate);
                        }
                    }

                    // Update the statistics
                    Interlocked.Add(ref processed, duplicates.Count - 1);
                    Interlocked.Add(ref bytes, filesize * (duplicates.Count - 1));
                    progressBar.Report((double)Interlocked.Increment(ref i) / count);
                });
            }

            // Display the statistics
            stopwatch.Stop();
            Console.Write(Environment.NewLine);
            foreach (string info in new[]
            {
                $"Elapsed time: \t\t{stopwatch.Elapsed:g}",
                $"Duplicates found: \t{processed}",
                $"Bytes identified: \t{bytes}",
                $"Approximate size: \t{bytes.ToFileSizeString()}"
            })
            {
                ConsoleHelper.WriteTaggedMessage(MessageType.Info, info);
            }
        }

        // Writes a complete log of the processed duplicates to the specified directory
        private static void WriteLog([NotNull] string path, [NotNull] ClupOptionsBase options, [NotNull, ItemNotNull] IEnumerable<string> duplicates)
        {
            string logfile = Path.Combine(path, $"logfile_{DateTime.Now:yyyy-mm-dd[hh-mm-ss]}.txt");
            using (StreamWriter writer = File.CreateText(logfile))
            {
                writer.WriteLine("========");
                writer.WriteLine(options.SourceDirectory);
                string[] extensions = options.FileExtensions.ToArray();
                if (extensions.Length > 0)
                {
                    string args = extensions.Length == 1 ? extensions[0] : $"{extensions[0]}{extensions.Skip(1).Aggregate(string.Empty, (seed, value) => $"{seed},{value}")}";
                    writer.WriteLine($"--extensions={args}");
                }
                writer.WriteLine($"--minsize={options.MinSize}");
                writer.WriteLine($"--maxsize={options.MaxSize}");
                writer.WriteLine($"--match={options.Match}");
                writer.WriteLine("========");
                foreach (string duplicate in duplicates) writer.WriteLine(duplicate);
            }
        }

        #endregion
    }
}
