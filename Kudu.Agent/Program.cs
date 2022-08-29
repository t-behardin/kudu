using Kudu.Contracts.Jobs;
using Kudu.Contracts.Settings;
using Kudu.Core;
using Kudu.Core.Helpers;
using Kudu.Core.Hooks;
using Kudu.Core.Infrastructure;
using Kudu.Core.Jobs;
using Kudu.Core.Tracing;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;

namespace Kudu.Agent
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Trace.AutoFlush = true;
            Trace.WriteLine("Kudu Agent");

            // StdOut/StdErr is piped by the ContainerProcess class in DWASSVC
            Trace.Listeners.Add(new ConsoleTraceListener());

            Host.Run(args);

        }
    }
}
