using SmartBox.Interfaces;
using SmartBox.Services;
using SmartBox.ViewModels;
using SmartBox.Views;

namespace SmartBox
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            services.AddSingleton<AppShell>();
            services.AddSingleton<App>();

            services.AddSingleton<DictionaryPage>();
            services.AddSingleton<ParserPage>();

            services.AddTransient<AppShellViewModel>();
            services.AddTransient<DictionaryPageViewModel>();
            services.AddTransient<ParserPageViewModel>();

            services.AddSingleton<IThemeService, ThemeService>();

            return services;
        }
    }
}
