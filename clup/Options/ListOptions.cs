using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the command to list duplicate files into a target directory
    /// </summary>
    [Verb("list", HelpText = "Find duplicate files and list them into a text file in the specified directory")]
    internal sealed class ListOptions : ClupOptionsBase
    {
        [Option('t', "target", HelpText = "The target directory to use to create the list file. If not specified, the source directory will be used.", Required = false)]
        public string TargetDirectory { get; set; }
    }
}
