using System;
using clup.Options;
using CommandLine;

namespace clup
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<DeleteOptions, MoveOptions>(args).MapResult(
                (DeleteOptions options) => 0,
                (MoveOptions options) => 0,
                errors => 1);

            Console.ReadKey();
        }
    }
}
