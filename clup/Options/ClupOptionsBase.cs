using System.Collections.Generic;
using clup.Enums;
using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// The base class for all the available commands
    /// </summary>
    internal abstract class ClupOptionsBase
    {
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
}
