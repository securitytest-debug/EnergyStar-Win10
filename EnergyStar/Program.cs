using EnergyStar.Interop;
using System.Runtime.InteropServices;

namespace EnergyStar
{
    internal class Program
    {   
        public delegate bool ConsoleCtrlDelegate(int CtrlType);  
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);
        static bool HandlerRoutine(int ctrlType)
        {
            Console.WriteLine($"bye");
            cts.Cancel();
            HookManager.UnsubscribeWindowEvents();
            EnergyManager.RecoverAllUserProcesses();
            return true;
        }
        
        static CancellationTokenSource cts = new CancellationTokenSource();

        static async void HouseKeepingThreadProc()
        {
            Console.WriteLine("House keeping thread started.");
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var houseKeepingTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                    await houseKeepingTimer.WaitForNextTickAsync(cts.Token);
                    EnergyManager.ThrottleAllUserBackgroundProcesses();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        static void Main(string[] args)
        {
            // Well, this program only works for Windows Version starting with Cobalt...
            // Nickel or higher will be better, but at least it works in Cobalt
            //
            // In .NET 5.0 and later, System.Environment.OSVersion always returns the actual OS version
            SetConsoleCtrlHandler(new ConsoleCtrlDelegate(HandlerRoutine), true);
            HookManager.SubscribeToWindowEvents();
            EnergyManager.ThrottleAllUserBackgroundProcesses();

            var houseKeepingThread = new Thread(new ThreadStart(HouseKeepingThreadProc));
            houseKeepingThread.Start();

            while (true)
            {
                if (Event.GetMessage(out Win32WindowForegroundMessage msg, IntPtr.Zero, 0, 0))
                {
                    Event.TranslateMessage(ref msg);
                    Event.DispatchMessage(ref msg);
                }
            }
        }
    }
}
