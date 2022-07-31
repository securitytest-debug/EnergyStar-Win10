using EnergyStar.Interop;

namespace EnergyStar
{
    public delegate bool ControlCtrlDelegate(int CtrlType);  
    internal class Program
    {        
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);
        private const int CtrlCEvent = 0;//CTRL_C_EVENT = 0;//一个Ctrl+C的信号被接收，该信号或来自键盘，或来自GenerateConsoleCtrlEvent    函数   
        private const int CtrlBreakEvent = 1;//CTRL_BREAK_EVENT = 1;//一个Ctrl+Break信号被接收，该信号或来自键盘，或来自GenerateConsoleCtrlEvent    函数  
        private const int CtrlCloseEvent = 2;//CTRL_CLOSE_EVENT = 2;//当用户系统关闭Console时，系统会发送此信号到此   
        private const int CtrlLogoffEvent = 5;//CTRL_LOGOFF_EVENT = 5;//当用户退出系统时系统会发送这个信号给所有的Console程序。该信号不能显示是哪个用户退出。   
        private const int CtrlShutdownEvent = 6;//CTRL_SHUTDOWN_EVENT = 6;//当系统将要关闭时会发送此信号到所有Console程序   
        bool HandlerRoutine(int ctrlType)
        {
            Console.WriteLine("Set    SetConsoleCtrlHandler    success!!");
            switch (ctrlType)
            {
                case CtrlCEvent: System.Console.WriteLine("Ctrl+C keydown"); break;

                case CtrlBreakEvent: System.Console.WriteLine("Ctrl+Break keydown"); break;

                case CtrlCloseEvent: System.Console.WriteLine("window closed"); break;

                case CtrlLogoffEvent: System.Console.WriteLine("log off or shut down"); break;

                case CtrlShutdownEvent: System.Console.WriteLine("system shut down"); break;

                default: System.Console.WriteLine(ctrlType.ToString()); break;
            }
            cts.Cancel();
            HookManager.UnsubscribeWindowEvents();
            EnergyManager.RecoverAllUserProcesses();
            Console.WriteLine($"bye");
            return false;
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
                catch (TaskCanceledException)
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
