using Application;
using CommunityToolkit.Maui;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Serilog;
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

            #region Logging Configuration
            string logsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppInfo.Name, "Logs", ".log");

            string logsDirectory = Path.GetDirectoryName(logsPath)!;
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
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

            builder.Services.AddUIServices();
            builder.Services.AddInfrastructureServices();
            builder.Services.AddApplicationServices();

            builder.UseMauiApp(sp => sp.GetRequiredService<App>());

            return builder.Build();
        }
    }
}
