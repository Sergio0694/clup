﻿using System;
using System.Threading;
using clup.Core;
using clup.Options;
using CommandLine;

namespace clup
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // Setup
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("==== START ====");
            int code;
            bool beep = false;

            // Try to execute the requested action
            try
            {
                ParserResult<object> result = Parser.Default.ParseArguments<DeleteOptions, MoveOptions, ListOptions>(args);
                result.WithParsed<ClupOptionsBase>(options => beep = options.Beep);
                code = result.MapResult(
                    (DeleteOptions options) => { ClupEngine.Run(options); return 0; },
                    (MoveOptions options) => { ClupEngine.Run(options); return 0; },
                    (ListOptions options) => { ClupEngine.Run(options); return 0;},
                    errors => 1);
            }
#if DEBUG
            catch (Exception e)
            {
                System.Diagnostics.ExceptionExtentions.Demystify(e);
                Console.WriteLine($"{e.StackTrace}{Environment.NewLine}{e.GetType()} - {e.Message}");
                code = 1;
            }
#else
            catch
            {
                code = 1;
            }
#endif

            // Exit code feedback
            if (code == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"{Environment.NewLine}==== DONE ====");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"{Environment.NewLine}==== ERROR ====");
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

            return code;
        }
    }
}
