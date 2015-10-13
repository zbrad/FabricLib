using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ZBrad.FabricLib.Utilities
{
    /// <summary>
    /// utility methods
    /// </summary>
    public static class Debug
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// wait for debugger to attach to process
        /// </summary>
        public static void Wait()
        {
            DateTime start = DateTime.UtcNow;
            while (!Debugger.IsAttached)
            {
                log.Info("Waiting for debugger");
                Thread.Sleep(Defaults.WaitDelay);

                if ((DateTime.UtcNow - start) > Defaults.WaitMaximum)
                {
                    log.Info("Debugger did not attach. Continuing");
                    break;
                }
            }

            log.Info("Debugger Attached");
        }

        /// <summary>
        /// wait for debugger attach with timeout
        /// </summary>
        /// <param name="waitDuration">timeout duration</param>
        public static void Wait(TimeSpan waitDuration)
        {
            DateTime start = DateTime.UtcNow;
            while (!Debugger.IsAttached)
            {
                log.Info("Waiting for debugger");
                Thread.Sleep(Defaults.WaitDelay);

                if ((DateTime.UtcNow - start) > waitDuration)
                {
                    log.Info("Debugger did not attach. Continuing");
                    break;
                }
            }
        }

    }
}
