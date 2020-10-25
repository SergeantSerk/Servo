using System;
using System.Threading;

namespace Servo
{
    public class Program
    {
        private static Entry entry;

        public const ulong DeveloperId = 98486055170215936;

        public static bool QuitSignal = false;

        public static void Main()
        {
            AppDomain.CurrentDomain.ProcessExit += (o, e) => QuitSignal = true;

            try
            {
                using (entry = new Entry())
                {
                    entry.StartAsync().GetAwaiter().GetResult();

                    while (entry.IsActive && !QuitSignal)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    Console.WriteLine("\n" + (Console.Title = "Caught exit, quitting..."));
                }
            }
            catch (Exception e)
            {
                PrintError(e);
            }
        }

        public static void PrintError(object message)
        {
            Print(message.ToString(), ConsoleColor.Red, ConsoleColor.Black);
        }

        private static void Print(string message, ConsoleColor foreground, ConsoleColor background)
        {
            var previous = new Tuple<ConsoleColor, ConsoleColor>(Console.ForegroundColor, Console.BackgroundColor);
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.WriteLine(message);
            Console.ForegroundColor = previous.Item1;
            Console.BackgroundColor = previous.Item2;
        }
    }
}