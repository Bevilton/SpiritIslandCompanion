using Application.Behaviour;
using Application.Features.Friendships;
using Application.Features.Games;
using Application.Features.PlayerMerges;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application.Extensions;

public static class ServiceExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceExtensions).Assembly, includeInternalTypes: true);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(ServiceExtensions).Assembly);

            // Outermost first. Logging wraps everything, so a request turned away by validation
            // is still on record; saving is innermost, so it only ever follows a handler that ran.
            config.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            config.AddOpenBehavior(typeof(SaveChangesBehaviour<,>));
        });

        // The clock the handlers stamp things with. Registered with TryAdd so a host — or a
        // test — that has its own keeps it.
        services.TryAddSingleton(TimeProvider.System);

        // The read and write helpers that several handlers share — scoped like the context they wrap.
        services.AddScoped<GameSetupFactory>();
        services.AddScoped<MergeSubjectGames>();
        services.AddScoped<MergeRequestInbox>();
        services.AddScoped<SeatNameResolver>();

        // Transient, not scoped: a splitter remembers the local players it creates during one
        // split, so each friendship removal has to get its own.
        services.AddTransient<SharedGameSplitter>();
    }
}
