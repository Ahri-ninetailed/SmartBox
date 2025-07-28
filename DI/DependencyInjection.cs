using Microsoft.Extensions.DependencyInjection;

namespace DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAllServices(this IServiceCollection services)
        {
            return services;
        }
    }
}
