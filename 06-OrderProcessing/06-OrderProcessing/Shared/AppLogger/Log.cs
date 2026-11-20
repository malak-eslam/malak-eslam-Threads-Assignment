using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Shared.AppLogger;
public static class Log
{
    private static readonly object _lock = new();

    public static void Info(string source, string message) { Write(source, message, ConsoleColor.DarkGray); }
    public static void Success(string source, string message) { Write(source, message, ConsoleColor.Green); }
    public static void Error(string source, string message) { Write(source, message, ConsoleColor.Red); }

    private static void Write(
    string source,
    string message,
    ConsoleColor color)
    {
        lock (_lock)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color;

                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    $"[Thread {Environment.CurrentManagedThreadId}] " +
                    $"{source} -> {message}");
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }


}
