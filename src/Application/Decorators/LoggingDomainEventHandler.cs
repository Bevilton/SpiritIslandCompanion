using Domain.Primitives;
using Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Decorators;

public class LoggingDomainEventHandler<T> : IDomainEventHandler<T> where T : IDomainEvent
{
    private readonly INotificationHandler<T> _decorated;
    private readonly ILogger<LoggingDomainEventHandler<T>> _logger;

    public LoggingDomainEventHandler(INotificationHandler<T> decorated, ILogger<LoggingDomainEventHandler<T>> logger)
    {
        _decorated = decorated;
        _logger = logger;
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling '{DomainEventHandlerType}' for domain event '{DomainEvent}'", _decorated.GetType().Name, notification);

        await _decorated.Handle(notification, cancellationToken);

        _logger.LogInformation("Handled '{DomainEventHandlerType}' for domain event '{DomainEvent}'", _decorated.GetType().Name, notification.GetType().Name);
    }
}