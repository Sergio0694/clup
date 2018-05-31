using System;
using CommandLine;

namespace clup.Options
{
    /// <summary>
    /// A class that represents the command to list duplicate files into a target directory
    /// </summary>
    [Verb("list", HelpText = "Find duplicate files and list them into a text file in the specified directory")]
    internal sealed class ListOptions : ClupOptionsBase
    {
        [Option('t', "target", HelpText = "The optional target directory to use to create the list file. If not specified, the source directory will be used.", Required = false)]
        public string TargetDirectory { get; set; }

        [Option("target-root", Default = false, HelpText = "Shortcut to set the target directory as the same directory used as source.", Required = false)]
        public bool TargetRoot { get; set; }

        /// <inheritdoc/>
        public override void Validate()
        {
            base.Validate();
            if (TargetRoot && !string.IsNullOrEmpty(TargetDirectory))
                throw new ArgumentException("The --target-root and --target options can't be used at the same time");
        }
    }
}
