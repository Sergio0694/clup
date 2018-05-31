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
        [Option('i', "include", HelpText = "The list of file extensions to look for when scanning the target directory. If not specified, all existing files will be analyzed.", Required = false, Separator = ',')]
        public IEnumerable<string> FileExtensions { get; set; }

        [Option('e', "exclude", HelpText = "The list of optional file extensions to filter out, when no other file extensions are specified.", Required = false, Separator = ',')]
        public IEnumerable<string> FileExclusions { get; set; }

        [Option('m', "minsize", Default = 0, HelpText = "The minimum size of files to be analyzed.", Required = false)]
        public long MinSize { get; set; }

        [Option('M', "maxsize", Default = 104_857_600, HelpText = "The maximum size of files to be analyzed.", Required = false)]
        public long MaxSize { get; set; }

        [Option('h', "match", Default = MatchMode.MD5, HelpText = "The desired mode to match duplicate files.", Required = false)]
        public MatchMode Match { get; set; }

        [Option('s', "source", HelpText = "The source directory to use to look for duplicates.", Required = true)]
        public string SourceDirectory { get; set; }

        [Option('b', "beep", Default = false, HelpText = "Play a sound when the requested operation completes.", Required = false)]
        public bool Beep { get; set; }

        /// <summary>
        /// Executes a preliminary validation of the current instance
        /// </summary>
        [AssertionMethod]
        public virtual void Validate()
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            if (FileExtensions.Any(ext => ext.Any(c => invalid.Contains(c))))
                throw new ArgumentException("One or more file extensions are not valid");
            if (MinSize < 0) throw new ArgumentException("The minimum file size must be a positive number");
            if (MaxSize <= MinSize) throw new ArgumentException("The maximum size must be greater than the minimum size");
            if (string.IsNullOrEmpty(SourceDirectory)) throw new ArgumentException("The source directory can't be empty");
            invalid = Path.GetInvalidPathChars();
            if (SourceDirectory.Any(c => invalid.Contains(c))) throw new ArgumentException("The source directory isn't valid");
            if (!Directory.Exists(SourceDirectory)) throw new ArgumentException("The source directory doesn't exist");
            if (FileExtensions.Any() && FileExclusions.Any())
                throw new ArgumentException("The list of extensions to exclude must be empty when other extensions to look for are specified");
        }
    }
}
