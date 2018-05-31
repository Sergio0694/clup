using clup.Options.Abstract;
using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the base command to delete duplicate files from a specified directory
    /// </summary>
    [Verb("delete", HelpText = "Automatically delete the duplicate files that are found in the target directory")]
    internal sealed class DeleteOptions : ClupOptionsWithLogBase { }
}
