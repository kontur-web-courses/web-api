namespace WebApi.MinimalApi.Domain;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddRepositories(this IServiceCollection services) =>
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
}