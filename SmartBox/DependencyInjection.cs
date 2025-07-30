using Mapster;
using SmartBox.Interfaces;
using SmartBox.MappingConfigs;
using SmartBox.Services;
using SmartBox.ViewModels;
using SmartBox.Views;

namespace SmartBox
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;

            new AppThemeMappingConfig().Register(config);
            new WordMappingConfig().Register(config);

            services.AddSingleton(config);

            services.AddSingleton<AppShell>();
            services.AddSingleton<App>();

            services.AddSingleton<DictionaryPage>();
            services.AddSingleton<ParserPage>();

            services.AddTransient<AppShellViewModel>();
            services.AddTransient<DictionaryPageViewModel>();
            services.AddTransient<ParserPageViewModel>();

            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IAlertService, AlertService>();

            return services;
        }
    }
}
