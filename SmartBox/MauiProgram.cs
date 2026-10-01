using Application;
using CommunityToolkit.Maui;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using SmartBox.Services;
using Syncfusion.Maui.Core.Hosting;

namespace SmartBox
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .ConfigureSyncfusionCore()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AppInfo.Name);
            Directory.CreateDirectory(appDataDir);

            // Логирование настраиваем первым, чтобы в лог попадали и ошибки чтения конфига
            #region Logging Configuration
            string logsPath = Path.Combine(appDataDir, "Logs", ".log");

            string logsDirectory = Path.GetDirectoryName(logsPath)!;
            Directory.CreateDirectory(logsDirectory);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(logsPath,
                              rollingInterval: RollingInterval.Day,
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
                .CreateLogger();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Logging.AddSerilog(Log.Logger);

            #endregion

#if WINDOWS
            WinUI.FatalErrorHandler.Register(logsDirectory);
#endif

            ConfigMerger.EnsureUserConfig(Path.Combine(AppContext.BaseDirectory, "config.json"),
                                          Path.Combine(appDataDir, "config.json"));

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(appDataDir)
                .AddJsonFile("config.json", optional: false)
                .Build();

            builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        window.ExtendsContentIntoTitleBar = false;
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                        int width = config.GetValue<int>("Window:Width");
                        int height = config.GetValue<int>("Window:Height");

                        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

                        // Опционально: установить позицию окна по центру экрана
                        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(id, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                        if (displayArea != null)
                        {
                            var centerX = (displayArea.WorkArea.Width - width) / 2;
                            var centerY = (displayArea.WorkArea.Height - height) / 2;
                            appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                        }
                    });
                });
#endif
            });

            builder.Services.AddUIServices();
            builder.Services.AddInfrastructureServices(config, Path.Combine(appDataDir, "Backups"));
            builder.Services.AddApplicationServices(config);

            builder.UseMauiApp(sp => sp.GetRequiredService<App>());

            return builder.Build();
        }
    }
}
