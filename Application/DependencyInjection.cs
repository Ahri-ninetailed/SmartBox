using Application.Interfaces;
using Application.MappingConfigs;
using Application.Services;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;

            new PartOfSpeechMappingConfig().Register(config);
            new WordMappingConfig().Register(config);

            services.AddSingleton(config);

            services.AddSingleton<IDictionaryService, DictionaryService>();

            return services;
        }
    }
}
