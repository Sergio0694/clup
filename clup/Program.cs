using System;
using clup.Core;
using clup.Options;
using CommandLine;

namespace clup
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                // Try to execute the requested action
                return Parser.Default.ParseArguments<DeleteOptions, MoveOptions>(args).MapResult(
                    (DeleteOptions options) => { ClupEngine.Run(options); return 0; },
                    (MoveOptions options) => 0,
                    errors => 1);
            }
#if DEBUG
            catch (Exception e)
            {
                Console.WriteLine($"{e.StackTrace}{Environment.NewLine}{e.GetType()} - {e.Message}");
            }
#else
            catch
            {
                Console.WriteLine("Something went wrong :'(");
            }
#endif
            return 1;
        }
    }
}
