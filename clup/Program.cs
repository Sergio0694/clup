using System;
using System.Threading;
using clup.Core;
using clup.Enums;
using clup.Options;
using clup.Options.Abstract;
using CommandLine;

namespace clup
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // Setup
            ConsoleColor color = Console.ForegroundColor;
            int code;
            bool beep = false, parsed = false;

            // Try to execute the requested action
            try
            {
                ParserResult<object> result = Parser.Default.ParseArguments<DeleteOptions, MoveOptions, ListOptions>(args);

                // Only display ==== START ==== if the parsing is successful, to avoid changing colors for the --help auto-screen
                if (result.Tag == ParserResultType.Parsed)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    ConsoleHelper.WriteLine($"{Environment.NewLine}==== START ====");
                    parsed = true;
                }

                // Actual execution of the requested command
                result.WithParsed<ClupOptionsBase>(options => beep = options.Beep);
                code = result.MapResult(
                    (DeleteOptions options) => { ClupEngine.Run(options); return 0; },
                    (MoveOptions options) => { ClupEngine.Run(options); return 0; },
                    (ListOptions options) => { ClupEngine.Run(options); return 0;},
                    errors => 1);
            }
            catch (Exception e)
            {
                ConsoleHelper.WriteTaggedMessage(MessageType.Error, e.Message);
                code = 1;
            }

            // Exit code feedback
            if (code == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                ConsoleHelper.WriteLine("==== SUCCESS ====");
            }
            else if (parsed) // Avoid showing the error if the operation never actually started
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                ConsoleHelper.WriteLine("==== FAILURE ====");
            }
            Console.ForegroundColor = color; // Reset to the default color

            // Sound notification
            if (beep)
            {
                if (code == 0)
                {
                    Console.Beep(); Thread.Sleep(150); Console.Beep(); // Two high-pitched beeps to indicate success
                }
                else Console.Beep(320, 500);
            }

#if DEBUG
            Console.ReadKey();
#endif
            return code;
        }
    }
}
