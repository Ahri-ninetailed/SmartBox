using Shared;
using SmartBox.Interfaces;
using SmartBox.Mappers;
using SmartBox.Services;
using SmartBox.ViewModels;
using SmartBox.Views;
using Syncfusion.Maui.Themes;

namespace SmartBox
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            services.AddSingleton<IMapper<AppTheme, SfVisuals>, AppThemeMapper>();
            services.AddSingleton<IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word>, WordMapper>();

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
