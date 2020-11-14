using System;
using System.Threading;

namespace Servo
{
    public class Program
    {
        private static Entry entry;

        public static bool QuitSignal = false;
        public static bool RestartSignal = false;
        public const ulong DeveloperId = 98486055170215936;

        public static void Main()
        {
            AppDomain.CurrentDomain.ProcessExit += (o, e) => QuitSignal = true;

            try
            {
                while (true)
                {
                    using (entry = new Entry())
                    {
                        entry.StartAsync().GetAwaiter().GetResult();

                        while (entry.IsActive && (!QuitSignal && !RestartSignal))
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        if (QuitSignal && !RestartSignal)
                        {
                            Console.WriteLine("\n" + (Console.Title = "Caught exit, quitting..."));
                        }
                        else if (RestartSignal && !QuitSignal)
                        {
                            Console.WriteLine("\n" + (Console.Title = "Caught restart, restarting..."));
                        }
                    }

                    if (QuitSignal && !RestartSignal)
                    {
                        Console.WriteLine("\n" + (Console.Title = "Done, bye!"));
                        QuitSignal = false;
                        RestartSignal = false;
                        break;
                    }
                    else if (RestartSignal && !QuitSignal)
                    {
                        Console.WriteLine("\n" + (Console.Title = "Restarting bot..."));
                        QuitSignal = false;
                        RestartSignal = false;
                    }
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