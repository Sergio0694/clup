using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using clup.Options;
using JetBrains.Annotations;

namespace clup.Core
{
    internal static class ClupEngine
    {
        private static void Run([NotNull] ClupOptionsBase options, [NotNull] Action<string> handler)
        {
            // Initial arguments validation
            options.Validate();

            // Prepare the files query
            string[] extensions = options.FileExtensions.ToArray();
            string pattern = $"*.{extensions[0]}{extensions.Skip(1).Aggregate(string.Empty, (seed, value) => $"{seed} OR *.{value}")}";
            string[] files = Directory.EnumerateFiles(options.SourceDirectory, pattern, SearchOption.AllDirectories).ToArray();

            // Initialize the mapping between each target file and its MD5 hash
            ConcurrentDictionary<string, List<string>> map = new ConcurrentDictionary<string, List<string>>();
            Parallel.ForEach(files, file =>
            {
                using (MD5 md5 = MD5.Create())
                using (FileStream stream = File.OpenRead(file))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string hex = BitConverter.ToString(hash);
                    map.AddOrUpdate(hex, new List<string> { file }, (_, list) =>
                    {
                        list.Add(file);
                        return list;
                    });
                }
            });

            // Process each duplicate file that has been found
            Parallel.ForEach(map.Values, duplicates =>
            {
                if (duplicates.Count < 2) return;
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
            });
        }

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
    }
}
