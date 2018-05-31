using System;
using clup.Enums;

namespace clup.Core
{
    /// <summary>
    /// A small class with some helper methods to print info to the user
    /// </summary>
    internal static class ConsoleHelper
    {
        /// <summary>
        /// Shows a message to the user
        /// </summary>
        /// <param name="type">The type of message being displayed</param>
        /// <param name="message">The text of the message</param>
        public static void WriteTaggedMessage(MessageType type, string message)
        {
            switch (type)
            {
                case MessageType.Error:
                    WriteTaggedMessage(ConsoleColor.DarkYellow, "ERROR", message);
                    break;
                case MessageType.Info:
                    WriteTaggedMessage(ConsoleColor.DarkCyan, "INFO", message);
                    break;
                default: throw new ArgumentOutOfRangeException("Invalid message type");
            }
        }

        // Shows a tagged message to the user
        private static void WriteTaggedMessage(ConsoleColor errorColor, string tag, string message)
        {
            if (Console.CursorLeft > 0) Console.WriteLine();
            Console.ForegroundColor = errorColor;
            Console.Write($"[{tag}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
    }
}
