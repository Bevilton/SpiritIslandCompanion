using Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Interceptors;

public sealed class ProcessEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessEventsInterceptor> _logger;

    public ProcessEventsInterceptor(IPublisher publisher, ILogger<ProcessEventsInterceptor> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var depth = 10;

        while (depth > 0)
        {
            var domainEvents = GetEvents(dbContext).ToList();

            if (domainEvents.Count == 0)
                break;

            foreach (var domainEvent in domainEvents.Distinct())
            {
                _logger.LogInformation("Publishing domain event {DomainEvent}", domainEvent.GetType().Name);
                await _publisher.Publish(domainEvent, cancellationToken);
                _logger.LogInformation("Domain event {DomainEvent} published", domainEvent.GetType().Name);
            }

            depth--;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private IEnumerable<IDomainEvent> GetEvents(DbContext dbContext)
    {
        var domainEvents = new List<IDomainEvent>();

        foreach (var entityEntry in dbContext.ChangeTracker.Entries<IHasEvents>().Select(x => x.Entity))
        {
            var events = entityEntry.GetDomainEvents();
            domainEvents.AddRange(events);
            entityEntry.ClearEvents();
        }

        return domainEvents;
    }
}