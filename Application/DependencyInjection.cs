using Application.Interfaces;
using Application.Mapper;
using Application.Mappers;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IMapper<Domain.Models.Dictionary.Word, Models.Dictionary.Word>, WordMapper>();
            services.AddSingleton<IMapper<Models.Dictionary.Word, Domain.Models.Dictionary.Word>, WordMapper>();
            services.AddSingleton<IMapper<Domain.Models.Parser.Word, Models.Parser.Word>, WordMapper>();

            services.AddSingleton<IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech>, PartOfSpeechMapper>();

            services.AddSingleton<IDictionaryService, DictionaryService>();
            services.AddSingleton<IParserService, ParserService>();

            return services;
        }
    }
}
