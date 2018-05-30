using System;
using System.Diagnostics;
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
                return Parser.Default.ParseArguments<DeleteOptions, MoveOptions, ListOptions>(args).MapResult(
                    (DeleteOptions options) => { ClupEngine.Run(options); return 0; },
                    (MoveOptions options) => { ClupEngine.Run(options); return 0; },
                    (ListOptions options) => { ClupEngine.Run(options); return 0;},
                    errors => 1);
            }
#if DEBUG
            catch (Exception e)
            {
                e.Demystify();
                Console.WriteLine($"{e.StackTrace}{Environment.NewLine}{e.GetType()} - {e.Message}");
                Console.ReadKey();
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
