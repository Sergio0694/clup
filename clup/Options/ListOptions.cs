using clup.Options.Abstract;
using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the command to list duplicate files into a target directory
    /// </summary>
    [Verb("list", HelpText = "Find duplicate files and list them into a text file in the specified directory")]
    internal sealed class ListOptions : ClupOptionsWithLogBase { }
}
