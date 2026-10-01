using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Domain.Mappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Мапперы между контекстами Parser и Dictionary из Domain, ими пользуются сервисы этого слоя
            services.AddSingleton<IMapper<Domain.Models.Dictionary.Definition, Domain.Models.Parser.Definition>, DefinitionMapper>();
            services.AddSingleton<IMapper<Domain.Models.Parser.Definition, Domain.Models.Dictionary.Definition>, DefinitionMapper>();
            services.AddSingleton<IMapper<Domain.Models.Dictionary.PartOfSpeech, Domain.Models.Parser.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Domain.Models.Dictionary.Word, Domain.Models.Parser.Word>, WordMapper>();

            services.AddSingleton(new AiSettings(config.GetValue<int>("Ai:FillMissingExamplesIfBelow"),
                                                 config.GetValue<int>("Ai:ExamplesPerMeaningForFilling"),
                                                 config.GetValue<int>("Ai:ExamplesPerMeaningForNewWords")));

            int backupIntervalHours = config.GetValue<int?>("Backup:IntervalHours") ?? throw new InvalidOperationException("Backup:IntervalHours is not set in config");
            int backupKeepCount = config.GetValue<int?>("Backup:KeepCount") ?? throw new InvalidOperationException("Backup:KeepCount is not set in config");
            services.AddSingleton(new BackupSettings(TimeSpan.FromHours(backupIntervalHours), backupKeepCount));

            services.AddSingleton<IDictionaryService, DictionaryService>();
            services.AddSingleton<IParserService, ParserService>();
            services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

            return services;
        }
    }
}
