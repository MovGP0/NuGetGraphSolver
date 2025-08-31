using Microsoft.Extensions.DependencyInjection;

namespace NuGetGraphSolver;

public static class ServicesRegistration
{
    public static void AddCoreServices(IServiceCollection services)
    {
        services.AddSingleton<ApplicationRunner>();
    }
}