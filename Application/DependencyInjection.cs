using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IDictionaryService, DictionaryService>();
            services.AddSingleton<IParserService, ParserService>();

            return services;
        }
    }
}
