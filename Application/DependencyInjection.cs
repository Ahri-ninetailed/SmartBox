using Application.Interfaces;
using Application.Mapper;
using Application.Mappers;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IMapper<Domain.Models.Word, Word>, WordMapper>();
            services.AddSingleton<IMapper<Word, Domain.Models.Word>, WordMapper>();

            services.AddSingleton<IMapper<Domain.Models.PartOfSpeech, PartOfSpeech>, PartOfSpeechMapper>();
            services.AddSingleton<IMapper<PartOfSpeech, Domain.Models.PartOfSpeech>, PartOfSpeechMapper>();


            services.AddSingleton<IDictionaryService, DictionaryService>();

            return services;
        }
    }
}
