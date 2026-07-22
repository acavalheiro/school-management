using Application.Common.Behaviors;
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
        {
            // The concrete handler is registered by its own type so the decorator
            // can resolve it without recursing through IRequestHandler<,>.
            services.AddScoped(handler.HandlerType);

            var args = handler.Interface.GetGenericArguments();
            var behaviorType = typeof(ValidationBehavior<,>).MakeGenericType(args[0], args[1]);
            var setInner = behaviorType.GetMethod(nameof(ValidationBehavior<IRequest<object>, object>.SetInner))!;
            var handlerType = handler.HandlerType;

            // Resolving IRequestHandler<,> yields the validation decorator wrapping
            // the real handler, so validators run before any handler executes.
            services.AddScoped(handler.Interface, sp =>
            {
                var behavior = ActivatorUtilities.CreateInstance(sp, behaviorType);
                setInner.Invoke(behavior, [sp.GetRequiredService(handlerType)]);
                return behavior;
            });
        }

        // Register all validators from Application assembly
        services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Scoped);

        return services;
    }
}
