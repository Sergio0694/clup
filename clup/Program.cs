using System;
using System.Collections.Generic;
using CommandLine;

namespace clup
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    [Verb("delete", HelpText = "Automatically delete the duplicate files that are found in the target directory")]
    internal sealed class DeleteOptions : ClupOptionsBase
    {
        [Option(HelpText = "An optional directory to use to store a log file", Required = false)]
        public string LogDir { get; set; }

        [Option(Default = false, HelpText = "Indicates whether or not to permanently remove duplicate files", Required = false)]
        public bool SkipRecycleBin { get; set; }
    }

    [Verb("move", HelpText = "Find duplicate files and move them to a specified directory instead of deleting them")]
    internal sealed class MoveOptions : ClupOptionsBase
    {
        [Value(1, HelpText = "The target directory to use to move duplicate files", Required = true)]
        public string TargetDir { get; set; }
    }

    internal abstract class ClupOptionsBase
    {
        /// <summary>
        /// Gets the list of file extensions to filter
        /// </summary>
        [Option("extensions", HelpText = "The list of file extensions to look for when scanning the target directory. If not specified, all existing files will be analyzed.", Required = false, Separator = ',')]
        public IEnumerable<string> FileExtensions { get; set; }

        [Option(HelpText = "The minimum size of files to be analyzed.", Required = false)]
        public long MinSize { get; set; }

        [Option(HelpText = "The maximum size of files to be analyzed")]
        public long MaxSize { get; set; }

        [Option(Default = MatchMode.MD5, HelpText = "The desired mode to match duplicate files", Required = false)]
        public MatchMode Mode { get; set; }

        [Value(0, HelpText = "The source directory to use to look for duplicates", Required = true)]
        public string SourceDirectory { get; set; }
    }

    internal enum MatchMode
    {
        MD5,
        MD5AndExtension,
        MD5AndFilename
    }
}
