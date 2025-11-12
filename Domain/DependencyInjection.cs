using Domain.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDomainMappers(this IServiceCollection services)
        {
            // Mappers between Parser and Dictionary contexts
            services.AddSingleton<IMapper<Models.Dictionary.Definition, Models.Parser.Definition>, DefinitionMapper>();
            services.AddSingleton<IMapper<Models.Parser.Definition, Models.Dictionary.Definition>, DefinitionMapper>();
            services.AddSingleton<IMapper<Models.Dictionary.PartOfSpeech, Models.Parser.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Models.Dictionary.Word, Models.Parser.Word>, WordMapper>();

            return services;
        }
    }
}
