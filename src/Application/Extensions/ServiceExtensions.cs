using Application.Behaviour;
using Application.Decorators;
using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace Application.Extensions;

public static class ServiceExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceExtensions).Assembly, includeInternalTypes: true);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(ServiceExtensions).Assembly);
            config.NotificationPublisher = new ForeachAwaitPublisher();

            config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            config.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            config.AddOpenBehavior(typeof(SaveChangesBehaviour<,>));
        });

        services.TryDecorate(typeof(INotificationHandler<>), typeof(LoggingDomainEventHandler<>));
    }
}