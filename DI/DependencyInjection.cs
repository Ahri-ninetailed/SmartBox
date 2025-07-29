using Application;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAllServices(this IServiceCollection services)
        {
            services.AddInfrastructureServices();
            services.AddApplicationServices();

            return services;
        }
    }
}
