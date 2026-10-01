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
            services.AddSingleton<IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech>, PartOfSpeechMapper>();

            services.AddSingleton<IMapper<Domain.Models.Dictionary.Word, Models.Dictionary.Word>, WordMapper>();
            services.AddSingleton<IMapper<Domain.Models.Parser.Word, Models.Parser.Word>, WordMapper>();
            services.AddSingleton<IMapper<Models.Parser.WordPack, Domain.Models.Parser.Word>, WordMapper>();

            services.AddSingleton<IMapper<SmartBox.Models.Parser.Meaning, Domain.Models.Dictionary.Definition>, DefinitionMapper>();
            services.AddSingleton<IMapper<SmartBox.Models.Parser.Meaning, Domain.Models.Parser.Definition>, DefinitionMapper>();

            services.AddSingleton<App>();

            services.AddSingleton<AppShell>();
            services.AddTransient<AppShellViewModel>();
            services.AddSingleton<DatabaseBackupViewModel>();

            services.AddSingleton<DictionaryPage>();
            services.AddSingleton<ParserPage>();
            services.AddSingleton<DefinitionsPage>();

            services.AddTransient<DictionaryPageViewModel>();
            services.AddTransient<ParserPageViewModel>();
            services.AddTransient<DefinitionsPageViewModel>();

            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IAlertService, AlertService>();
            services.AddSingleton<IClipboardService, ClipboardService>();

            return services;
        }
    }
}
