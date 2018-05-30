using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the base command to delete duplicate files from a specified directory
    /// </summary>
    [Verb("delete", HelpText = "Automatically delete the duplicate files that are found in the target directory")]
    internal sealed class DeleteOptions : ClupOptionsBase
    {
        [Option("logdir", HelpText = "An optional directory to use to store a log file", Required = false)]
        public string LogDirectory { get; set; }
    }
}
