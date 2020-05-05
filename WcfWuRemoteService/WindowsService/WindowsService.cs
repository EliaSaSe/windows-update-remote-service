/*
    Windows Update Remote Service
    Copyright(C) 2016-2020  Elia Seikritt

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/
using log4net;
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace WcfWuRemoteService.WindowsService
{
    /// <summary>
    /// <see cref="WindowsService"/> is the entrypoint for the windows update remote service. Can also run in interactive mode.
    /// This class is responsible for the interaction with the service control manager and uses 
    /// a <see cref="ServiceWorker"/> to do the work.
    /// </summary>
    public class WindowsService : ServiceBase
    {
        private System.ComponentModel.IContainer components = null;
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ServiceWorker _worker;
        private readonly int shutdownTimeout = 10000;

        private delegate bool EventHandler(CtrlType sig);

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        /// <summary>
        /// A reference to the event handler is required during the process life time. 
        /// This reference prevents GC collection.
        /// "'CallbackOnCollectedDelegate' : A callback was made on a garbage collected delegate of type ... 
        /// ... delegates to unmanaged code ... must be kept alive by the managed application ..."
        /// </summary>
        private static EventHandler eventHandler;

        private enum CtrlType
        {
            CtrlCEvent = 0,
            CtrlBreakEvent = 1,
            CtrlCloseEvent = 2,
            CtrlLogoffEvent = 5,
            CtrlShutdownEvent = 6
        }

        public WindowsService(string[] args)
        {
            components = new System.ComponentModel.Container();
            ServiceName = "Windows Update Remote Service";
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
            WindowsService service = new WindowsService(args);

            if (Environment.UserInteractive) // Run as console application
            {
                var shutdownEvent = new ManualResetEventSlim(false);
                eventHandler = new EventHandler(sig =>
                {
                    // https://docs.microsoft.com/en-us/windows/console/handlerroutine
                    Log.Info(@"Exiting process due event: " + sig.ToString());
                    service.OnStop();
                    shutdownEvent.Set();
                    Environment.Exit(0);
                    return true;
                });

                PrintBoilerplate();
                Log.Debug("Starting in interactive mode.");

                SetConsoleCtrlHandler(eventHandler, true);
                service.OnStart(args);
                shutdownEvent.Wait(); // Waiting shutdown event
            }
            else // Run as Windows service
            {
                Log.Debug("Starting in windows service mode.");
                Run(service);
            }
        }

        private static void PrintBoilerplate()
        {
            var color = Console.ForegroundColor;
            Console.WriteLine("Windows Update Remote Service Copyright(C) 2016 - 2020 Elia Seikritt");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
            Console.ForegroundColor = color;
            Console.WriteLine("Released under the GNU Lesser General Public License (lgpl - 3.0).");           
            Console.WriteLine("Start the client application and click \"About\" for more details");
            Console.WriteLine("and visit http://www.gnu.org/licenses/lgpl-3.0." + Environment.NewLine);
        }

        protected override void OnStart(string[] args)
        {
            Log.Debug($"Windows service control command: {nameof(OnStart)}.");
            if (_worker == null)_worker = new ServiceWorker(ServiceName);
            Task.Run(() => _worker.Start()); // OnStart should return as soon as possible, so execute the expensive startup process asynchronous
        }

        protected override void OnStop()
        {
            Log.Info($"Windows service control command: {nameof(OnStop)}.");
            InternalStop();
        }

        protected override void OnShutdown()
        {
            Log.Info($"Windows service control command: {nameof(OnShutdown)}.");
            InternalStop();
        }

        /// <summary>
        /// Stops the <see cref="_worker"/>.
        /// Will request additional time via <see cref="ServiceBase.RequestAdditionalTime(int)"/> when <see cref="shutdownTimeout"/> is exceeded.
        /// </summary>
        private void InternalStop()
        {
            if (_worker != null)
            {
                bool firstReqAddTime = true;
                var task = Task.Run(() => _worker.Stop());
                while (!task.Wait(shutdownTimeout))
                {
                    if (!Environment.UserInteractive)
                    {
                        try
                        {
                            RequestAdditionalTime(shutdownTimeout);
                            if (firstReqAddTime)
                            {
                                Log.Warn($"The service needs longer than {shutdownTimeout} ms to shutdown. " +
                                    $"The operating system may kill the service before a graceful shutdown was possible.");
                                firstReqAddTime = false;
                            }
                            Log.Warn($"Additional time to shutdown the service requested from the operating system: " +
                                $"{shutdownTimeout} ms");
                        }
                        catch (InvalidOperationException e)
                        {
                            Log.Warn("Request for additional shutdown time was rejected by the operating system.", e);
                        }
                    }
                    else if (firstReqAddTime)
                    {
                        Log.Warn($"The service needs longer than {shutdownTimeout} ms to shutdown.");
                        firstReqAddTime = false;
                    }
                }
            }
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal("Unhandled exception occurred.", e.ExceptionObject as Exception);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
