using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ZBrad.FabLibs.Utilities
{
    /// <summary>
    /// utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// wait for debugger to attach to process
        /// </summary>
        public static void WaitForDebugger()
        {
            DateTime start = DateTime.UtcNow;
            while (!Debugger.IsAttached)
            {
                Console.WriteLine("Waiting for debugger");
                Thread.Sleep(Defaults.WaitDelay);

                if ((DateTime.UtcNow - start) > Defaults.WaitMaximum)
                {
                    Console.WriteLine("Debugger did not attach. Continuing");
                    break;
                }
            }

            Console.WriteLine("Debugger Attached");
        }

        /// <summary>
        /// wait for debugger attach with timeout
        /// </summary>
        /// <param name="waitDuration">timeout duration</param>
        public static void WaitForDebugger(TimeSpan waitDuration)
        {
            DateTime start = DateTime.UtcNow;
            while (!Debugger.IsAttached)
            {
                Console.WriteLine("Waiting for debugger");
                Thread.Sleep(Defaults.WaitDelay);

                if ((DateTime.UtcNow - start) > waitDuration)
                {
                    Console.WriteLine("Debugger did not attach. Continuing");
                    break;
                }
            }
        }

        /// <summary>
        /// coding assertion
        /// </summary>
        /// <param name="condition">condition to test</param>
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                throw Utility.CodingError();
            }
        }

        /// <summary>
        /// coding assertion
        /// </summary>
        /// <param name="condition">condition to test</param>
        /// <param name="format">format of message</param>
        /// <param name="args">optional message arguments</param>
        public static void Assert(bool condition, string format, params object[] args)
        {
            if (!condition)
            {
                throw Utility.CodingError(format, args);
            }
        }

        /// <summary>
        /// create coding exception
        /// </summary>
        /// <returns>provides coding exception</returns>
        public static Exception CodingError()
        {
            return Utility.CodingError(string.Empty);
        }

        /// <summary>
        /// create coding exception
        /// </summary>
        /// <param name="format">format of message</param>
        /// <param name="args">optional message arguments</param>
        /// <returns>coding exception</returns>
        public static Exception CodingError(string format, params object[] args)
        {
            var text = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
            Console.WriteLine("{0}", text);
            Console.WriteLine("Press Enter to FailFast this process.");
            Console.ReadLine();
            Environment.FailFast(text);

            // should never reach this line
            return new InvalidOperationException(text);
        }

        /// <summary>
        /// provide state corruption indication
        /// </summary>
        /// <param name="exception">exception provided</param>
        public static void StateCorrupted(Exception exception)
        {
            Utility.CodingError("State corrupted because of {0}", exception);
        }

        /// <summary>
        /// prints a message in color
        /// </summary>
        /// <param name="message">text to use</param>
        /// <param name="color">color to display</param>
        public static void WriteWithColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
        }
    }
}
