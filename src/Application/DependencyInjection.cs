using Application.Common.Mediator;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register mediator
        services.AddScoped<IMediator, Mediator>();

        // Register all handlers from Application assembly
        var assembly = typeof(DependencyInjection).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && !t.ContainsGenericParameters)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new { HandlerType = t, Interface = i }));

        foreach (var handler in handlerTypes)
            services.AddScoped(handler.Interface, handler.HandlerType);

        // Register all validators from Application assembly
        services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Scoped);

        return services;
    }
}
