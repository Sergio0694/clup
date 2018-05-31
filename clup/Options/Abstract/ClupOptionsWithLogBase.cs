using System;
using CommandLine;

namespace clup.Options.Abstract
{
    /// <summary>
    /// A base class for a command that accepts an optional log directory
    /// </summary>
    internal abstract class ClupOptionsWithLogBase : ClupOptionsBase
    {
        [Option('l', "logdir", HelpText = "An optional directory to use to store a log file.", Required = false)]
        public string LogDirectory { get; set; }

        [Option("logdir-root", Default = false, HelpText = "Shortcut to set the log directory as the same directory used as source.", Required = false)]
        public bool LogDirectoryRoot { get; set; }

        /// <inheritdoc/>
        public override void Validate()
        {
            base.Validate();
            if (LogDirectoryRoot && !string.IsNullOrEmpty(LogDirectory))
                throw new ArgumentException("The --logdir-root and --logdir options can't be used at the same time");
        }
    }
}
