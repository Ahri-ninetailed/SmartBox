using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SmartBox.Interfaces;
using SmartBox.Services;
using SmartBox.ViewModels;
using SmartBox.Views;
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

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<App>();

            // 👇 Регистрируем View
            builder.Services.AddSingleton<DictionaryPage>();
            builder.Services.AddSingleton<ParserPage>();

            // 👇 Регистрируем ViewModel
            builder.Services.AddTransient<AppShellViewModel>();
            builder.Services.AddTransient<DictionaryPageViewModel>();
            builder.Services.AddTransient<ParserPageViewModel>();

            // 👇 Регистрируем UI Services
            builder.Services.AddSingleton<IThemeService, ThemeService>();

            builder.UseMauiApp(sp => sp.GetRequiredService<App>());

            return builder.Build();
        }
    }
}
