using Microsoft.Extensions.DependencyInjection;

namespace HomeManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
