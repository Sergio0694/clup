using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the command to list duplicate files into a target directory
    /// </summary>
    [Verb("list", HelpText = "Find duplicate files and list them into a text file in the specified directory")]
    internal sealed class ListOptions : ClupOptionsBase
    {
        [Value(1, HelpText = "The target directory to use to create the list file", Required = true)]
        public string TargetDir { get; set; }
    }
}
