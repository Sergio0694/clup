using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace clup.Core
{
    /// <summary>
    /// A small class with some useful extensions
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Converts a <see cref="long"/> into a string representing its file size
        /// </summary>
        /// <param name="value">The number of bytes to convert to a file size</param>
        [Pure]
        public static string ToFileSizeString(this long value)
        {
            if (value == 0) return "0 bytes";
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int unitsCount = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (unitsCount * 10));
            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[unitsCount]);
        }

        /// <summary>
        /// Concats the strings in the input sequence into a single one, using the given separator
        /// </summary>
        /// <param name="parts">The sequence of strings to merge</param>
        /// <param name="separator">The separator to use in the returned text</param>
        [Pure]
        public static string Concat(this IReadOnlyList<string> parts, char separator)
        {
            switch (parts.Count)
            {
                case 0: return string.Empty;
                case 1: return parts[0];
                default: return $"{parts[0]}{parts.Skip(1).Aggregate(string.Empty, (seed, value) => $"{seed}{separator}{value}")}";
            }
        }
    }
}
