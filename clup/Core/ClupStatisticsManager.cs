using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace clup.Core
{
    /// <summary>
    /// A class that calculates and prepares data statistics on a requested clup operation
    /// </summary>
    internal sealed class ClupStatisticsManager
    {
        // The timer to keep track of the elapsed time
        [NotNull]
        private readonly Stopwatch Stopwatch = new Stopwatch();

        // A map between each available file extensions and its relative data
        [NotNull]
        private readonly ConcurrentDictionary<string, (int Count, long Bytes)> SizeMap = new ConcurrentDictionary<string, (int, long)>();

        /// <summary>
        /// Gets a readonly map of all the identified duplicate files
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<string, IReadOnlyList<string>> DuplicatesMap => _DuplicatesMap;

        // A map of all the identified duplicate files
        [NotNull]
        private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _DuplicatesMap = new ConcurrentDictionary<string, IReadOnlyList<string>>();

        // The total number of duplicate files identified
        private int _Duplicates;

        // The total number of duplicate bytes identified
        private long _Bytes;

        /// <summary>
        /// Creates a new instance and automatically starts the internal timer
        /// </summary>
        public ClupStatisticsManager() => Stopwatch.Start();

        /// <summary>
        /// Adds a new list of duplicates to the internal statistics
        /// </summary>
        /// <param name="duplicates">The paths of the current batch of duplicate files</param>
        public void AddDuplicates([NotNull, ItemNotNull] IReadOnlyList<string> duplicates)
        {
            // Base case to ignore
            if (duplicates.Count == 0) throw new InvalidOperationException("The duplicates list can't be empty");
            if (duplicates.Count == 1) return;

            // Update the statistics based on the current set of duplicate files
            int pending = duplicates.Count - 1;
            Interlocked.Add(ref _Duplicates, pending);
            long filesize = new FileInfo(duplicates[0]).Length;
            Interlocked.Add(ref _Bytes, filesize * pending);
            SizeMap.AddOrUpdate(
                Path.GetExtension(duplicates[0]),
                (pending, filesize * pending),
                (_, value) => (value.Count + pending, value.Bytes + filesize * pending));
        }

        /// <summary>
        /// Adds a list of duplicate files, indicating their MD5 hash and moving the original one in the first position
        /// </summary>
        /// <param name="hash">The hash of the current group of duplicates</param>
        /// <param name="original">The path of the original file</param>
        /// <param name="duplicates">The list of duplicate files</param>
        public void AddDuplicates([NotNull] string hash, [NotNull] string original, [NotNull, ItemNotNull] IReadOnlyList<string> duplicates)
        {
            string[] ordered = duplicates.OrderByDescending(path => path.Equals(original)).ToArray();
            if (!_DuplicatesMap.TryAdd(hash, ordered)) throw new InvalidOperationException("Error adding the new key/value pair");
        }

        /// <summary>
        /// Stops the internal timer
        /// </summary>
        public void StopTracking() => Stopwatch.Stop();

        /// <summary>
        /// Prepares the statistics to display to the user
        /// </summary>
        /// <param name="verbose">Indicates whether or not to also include additional info</param>
        [Pure, NotNull, ItemNotNull]
        public IEnumerable<string> ExtractStatistics(bool verbose)
        {
            yield return $"Elapsed time:\t\t{Stopwatch.Elapsed:g}";
            yield return $"Duplicates found:\t{_Duplicates}";
            if (verbose) yield return $"Bytes identified:\t{_Bytes}";
            yield return $"Approximate size:\t{_Bytes.ToFileSizeString()}";
            if (verbose)
            {
                // Frequent file extensions
                var frequent = (
                    from pair in SizeMap
                    orderby pair.Value.Count descending
                    select (Key: pair.Key, Count: pair.Value.Count)).Take(5).ToArray();
                if (frequent.Length == 0) yield break;
                yield return "Frequent extensions:\t" + frequent.Skip(1).Aggregate(
                    $"{frequent[0].Key}: {frequent[0].Count}",
                    (seed, value) => $"{seed}, {value.Key}: {value.Count}");

                // Heaviest file extensions
                var heaviest = (
                    from pair in SizeMap
                    orderby pair.Value.Bytes descending
                    select (Key: pair.Key, Bytes: pair.Value.Bytes)).Take(5).ToArray();
                yield return "Heaviest extensions:\t" +  heaviest.Skip(1).Aggregate(
                    $"{heaviest[0].Key}: {heaviest[0].Bytes.ToFileSizeString()}",
                    (seed, value) => $"{seed}, {value.Key}: {value.Bytes.ToFileSizeString()}");
            }

        }
    }
}
