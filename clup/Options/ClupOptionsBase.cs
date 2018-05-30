using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using clup.Enums;
using CommandLine;
using JetBrains.Annotations;

namespace clup.Options
{
    /// <summary>
    /// The base class for all the available commands
    /// </summary>
    internal abstract class ClupOptionsBase
    {
        [Option("extensions", HelpText = "The list of file extensions to look for when scanning the target directory. If not specified, all existing files will be analyzed.", Required = false, Separator = ',')]
        public IEnumerable<string> FileExtensions { get; set; }

        [Option(Default = 0, HelpText = "The minimum size of files to be analyzed.", Required = false)]
        public long MinSize { get; set; }

        [Option(Default = 104_857_600, HelpText = "The maximum size of files to be analyzed", Required = false)]
        public long MaxSize { get; set; }

        [Option(Default = MatchMode.MD5, HelpText = "The desired mode to match duplicate files", Required = false)]
        public MatchMode Mode { get; set; }

        [Value(0, HelpText = "The source directory to use to look for duplicates", Required = true)]
        public string SourceDirectory { get; set; }

        /// <summary>
        /// Executes a preliminary validation of the current instance
        /// </summary>
        [AssertionMethod]
        public void Validate()
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            if (FileExtensions?.Any(ext => ext.Any(c => invalid.Contains(c))) == true)
                throw new ArgumentException("One or more file extensions are not valid");
            if (MinSize <= 0) throw new ArgumentException("The minimum file size must be a positive number");
            if (MaxSize <= MinSize) throw new ArgumentException("The maximum size must be greater than the minimum size");
            if (string.IsNullOrEmpty(SourceDirectory)) throw new ArgumentException("The source directory can't be empty");
            invalid = Path.GetInvalidPathChars();
            if (SourceDirectory.Any(c => invalid.Contains(c))) throw new ArgumentException("The source directory isn't valid");
            if (!Directory.Exists(SourceDirectory)) throw new ArgumentException("The source directory doesn't exist");
        }
    }
}
