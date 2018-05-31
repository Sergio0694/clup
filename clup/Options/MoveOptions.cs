using clup.Options.Abstract;
using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the command to move duplicate files into a target directory
    /// </summary>
    [Verb("move", HelpText = "Find duplicate files and move them to a specified directory instead of deleting them")]
    internal sealed class MoveOptions : ClupOptionsBase
    {
        [Option('t', "target", HelpText = "The target directory to use to move duplicate files.", Required = true)]
        public string TargetDirectory { get; set; }
    }
}
