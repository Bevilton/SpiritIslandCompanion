using Infrastructure.Demo;

namespace WebApp.Demo;

/// <summary>
/// Builds and seeds the demo template shortly after startup, then re-checks on a slow loop so
/// an expired template (its seeded history is anchored to the date it was built — see
/// <see cref="DemoSandboxRegistry.TemplateLifetime"/>) is rebuilt off the request path, and no
/// visitor pays for the seeding run. Purely an optimisation — the registry builds or refreshes
/// the template on demand if this hasn't run (or failed) by the time someone asks.
/// <para>
/// The same loop is what retires the sandboxes of visitors who have long since closed the tab:
/// the registry's other eviction pass only runs when a new visitor arrives.
/// </para>
/// </summary>
public sealed class DemoTemplateWarmup(
    DemoSandboxRegistry registry,
    ILogger<DemoTemplateWarmup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Off the startup path for real: the host runs ExecuteAsync inline until its first
        // genuine suspension, and seeding an in-memory SQLite database completes most of its
        // awaits synchronously — so without this the whole seeding run delays app start.
        await Task.Yield();

        // Frequent relative to the template's lifetime, so a rebuild lands close to the
        // moment it is due; EnsureTemplateAsync is a cheap no-op while the template is fresh.
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

        try
        {
            do
            {
                try
                {
                    await registry.EnsureTemplateAsync(stoppingToken);
                    await registry.EvictIdleAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Demo template warm-up failed; it will be retried on the next tick or on first demo entry");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutting down — nothing to salvage.
        }
    }
}
