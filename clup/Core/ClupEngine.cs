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
            if (string.IsNullOrEmpty(options.LogDir)) Run(options, File.Delete);
            else
            {
                ConcurrentQueue<string> deletions = new ConcurrentQueue<string>();
                Run(options, path =>
                {
                    deletions.Enqueue(path);
                    File.Delete(path);
                });
            }
        }

        /// <summary>
        /// Executes the move command
        /// </summary>
        /// <param name="options">The command options</param>
        public static void Run(MoveOptions options)
        {
            void Handler(string path)
            {
                string filename = Path.GetFileName(path);
                File.Move(path, Path.Combine(options.TargetDir, filename));
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
            string logfile = Path.Combine(options.TargetDir, $"logfile_{DateTime.Now:yyyy-mm-dd[hh-mm-ss]}.txt");
            using (StreamWriter writer = File.CreateText(logfile))
            {
                writer.WriteLine("========");
                writer.WriteLine(options.SourceDirectory);
                string[] extensions = options.FileExtensions.ToArray();
                string args = extensions.Length == 1 ? extensions[0] : $"{extensions[0]}{extensions.Skip(1).Aggregate(string.Empty, (seed, value) => $"{seed},{value}")}";
                writer.WriteLine($"--extensions={args}");
                writer.WriteLine($"--minsize={options.MinSize}");
                writer.WriteLine($"--maxsize={options.MaxSize}");
                writer.WriteLine($"--mode={options.Mode}");
                writer.WriteLine("========");
                foreach (string duplicate in duplicates) writer.WriteLine(duplicate);
            }
        }

        #endregion

        // Executes the requested command
        private static void Run([NotNull] ClupOptionsBase options, [NotNull] Action<string> handler)
        {
            // Stats
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int processed = 0;
            long bytes = 0;
            Console.WriteLine("==== START ====");

            // Initial arguments validation
            options.Validate();

            // Prepare the files query
            string[] extensions = options.FileExtensions.ToArray();
            string pattern = extensions.Length == 1 ? extensions[0] : $"*.{extensions[0]}{extensions.Skip(1).Aggregate(string.Empty, (seed, value) => $"{seed} OR *.{value}")}";
            string[] files = Directory.EnumerateFiles(options.SourceDirectory, pattern, SearchOption.AllDirectories).ToArray();

            // Initialize the mapping between each target file and its MD5 hash
            Console.WriteLine("Preprocessing files...");
            ConcurrentDictionary<string, List<string>> map = new ConcurrentDictionary<string, List<string>>();
            Parallel.ForEach(files, file =>
            {
                // Compute the MD5 hash
                using (MD5 md5 = MD5.Create())
                using (FileStream stream = File.OpenRead(file))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string hex = BitConverter.ToString(hash);

                    // Get the actual key for the current file
                    string key;
                    switch (options.Mode)
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
                }
            });

            // Process each duplicate file that has been found
            Console.WriteLine("Processing duplicates...");
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
                    int _i = Interlocked.Increment(ref i);
                    progressBar.Report((double)i / count);
                });
            }

            // Display the statistics
            stopwatch.Stop();
            Console.WriteLine(
                "==== DONE ====\n" +
                $"Elapsed time: {stopwatch.Elapsed:hh:mm:ss}" +
                $"Duplicates found/deleted: {processed}" +
                $"Bytes (potentially) saved: {bytes}");
        }
    }
}
