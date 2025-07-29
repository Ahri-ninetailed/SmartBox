using Application;
using CommunityToolkit.Maui;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

namespace SmartBox
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .ConfigureSyncfusionCore()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddUIServices();
            builder.Services.AddInfrastructureServices();
            builder.Services.AddApplicationServices();

            builder.UseMauiApp(sp => sp.GetRequiredService<App>());

            return builder.Build();
        }
    }
}
