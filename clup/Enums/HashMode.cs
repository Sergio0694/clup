namespace clup.Enums
{
    /// <summary>
    /// An enum that indicates how to match files to check if they are duplicates
    /// </summary>
    internal enum HashMode
    {
        /// <summary>
        /// Simple comparison of the MD5 hash of the file contents
        /// </summary>
        MD5,

        /// <summary>
        /// Comparison of both the MD5 hash and the file extension
        /// </summary>
        MD5AndExtension,

        /// <summary>
        /// Comparison of both the MD5 hash and the complete filename
        /// </summary>
        MD5AndFilename
    }
}
