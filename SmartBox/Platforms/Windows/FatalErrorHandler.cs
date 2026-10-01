using Serilog;
using System.Runtime.InteropServices;

namespace SmartBox.WinUI
{
    /// <summary>
    /// Пишет необработанные исключения в лог и перед закрытием приложения показывает окно с ошибкой
    /// </summary>
    public static class FatalErrorHandler
    {
        private const string Caption = "SmartBox";
        private const string FatalErrorMessage = "Unexpected error, the application will be closed.";

        private const uint MB_ICONERROR = 0x10;
        private const uint MB_SETFOREGROUND = 0x10000;
        private const uint MB_TOPMOST = 0x40000;

        private static string? _logsDirectory;
        private static int _reportingThreadId;

        public static void Register(string logsDirectory)
        {
            _logsDirectory = logsDirectory;

            // Исключения в UI-потоке, например в командах
            Microsoft.UI.Xaml.Application.Current.UnhandledException += (_, e) => Report(e.Exception);

            // Исключения в остальных потоках
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Report(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));

            // Исключения в задачах, которые никто не дождался, приложение не роняют, поэтому только пишем в лог
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log.Error(e.Exception, "Необработанное исключение в фоновой задаче");
                e.SetObserved();
            };
        }

        public static void Report(Exception exception)
        {
            // Одно падение может прийти из нескольких обработчиков. Окно показываем один раз,
            // а остальные потоки держим, пока первый не закроет приложение
            int currentThreadId = Environment.CurrentManagedThreadId;
            int reportingThreadId = Interlocked.CompareExchange(ref _reportingThreadId, currentThreadId, 0);
            if (reportingThreadId == currentThreadId)
            {
                // Новое исключение в том же потоке, пока открыто окно: если здесь уснуть, окно зависнет
                return;
            }
            if (reportingThreadId != 0)
            {
                Thread.Sleep(Timeout.Infinite);
            }

            try
            {
                Log.Fatal(exception, FatalErrorMessage);
                Log.CloseAndFlush();
            }
            catch
            {
                // Окно с ошибкой важнее, чем запись в лог
            }

            string message = $"{FatalErrorMessage}\n\n{exception.GetBaseException().Message}";
            if (_logsDirectory != null)
            {
                message += $"\n\nDetails are in the log: {_logsDirectory}";
            }

            // Нативное окно работает, даже когда интерфейс MAUI еще не создан или уже сломан
            MessageBox(IntPtr.Zero, message, Caption, MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST);

            Environment.Exit(1);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}
