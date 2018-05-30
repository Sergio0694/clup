using System;

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
        public static string ToFileSizeString(this long value)
        {
            if (value == 0) return "0 bytes";
            String[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int unitsCount = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (unitsCount * 10));
            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[unitsCount]);
        }
    }
}
