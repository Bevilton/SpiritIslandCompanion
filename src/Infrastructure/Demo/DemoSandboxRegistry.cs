using System.Collections.Concurrent;
using System.Data.Common;
using Application.Abstractions;
using Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Demo;

/// <summary>
/// The demo sandboxes: one throwaway in-memory SQLite database per demo visitor, all copied
/// from a single seeded template. Anything a visitor does in the demo — record a game, rename
/// the account, delete everything — lands in their own copy and nowhere else; the real
/// database is never touched from a demo scope, because <c>IAppDbContext</c> resolves through
/// here (see <c>ServiceExtensions.AddInfrastructure</c>).
/// <para>
/// Each sandbox is a named shared-cache in-memory database (<c>Mode=Memory;Cache=Shared</c>).
/// The registry holds one open keep-alive connection per sandbox — a shared in-memory database
/// lives exactly as long as at least one connection to it is open — and every scope opens its
/// own connection through EF, so concurrent circuits and endpoint requests never share a
/// connection object.
/// </para>
/// <para>
/// Sandboxes are evicted after sitting idle, and the visitor's cookie deliberately outlives
/// them: a returning visitor's sandbox is quietly rebuilt from the template under the same id,
/// which is also what makes the cookie survive process restarts.
/// </para>
/// <para>
/// The seeded history is anchored to "now" — it always ends yesterday — so the template
/// itself expires after <see cref="TemplateLifetime"/> and is reseeded, or a long-lived
/// process would slowly present drafts "from last week" that are months old. Live sandboxes
/// keep the dates they were copied with until they idle out; at worst a visitor's history
/// trails the calendar by the template's age plus their own session.
/// </para>
/// </summary>
public sealed class DemoSandboxRegistry(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoSandboxRegistry> logger) : IDisposable
{
    /// <summary>
    /// The template's own id in this registry — the seeding scope pins itself to it so the
    /// seeder's commands resolve <c>IAppDbContext</c> onto the template database. Never handed
    /// to a visitor and never evicted.
    /// </summary>
    public static readonly Guid TemplateId = Guid.Parse("d3300000-0000-4000-8000-000000007e97");

    /// <summary>A sandbox nobody has touched for this long is torn down.</summary>
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(45);

    /// <summary>
    /// How long a seeded template stays in service before it is rebuilt with the history
    /// re-anchored to the current date. A day keeps "yesterday's game" honest without ever
    /// reseeding while someone might notice.
    /// </summary>
    public static readonly TimeSpan TemplateLifetime = TimeSpan.FromHours(24);

    /// <summary>
    /// Hard cap on live sandboxes. A seeded copy is around a quarter of a megabyte, so the cap
    /// is worth well under a hundred megabytes of process memory; it exists so a crawler
    /// hammering the entry endpoint degrades old sandboxes instead of the process.
    /// </summary>
    private const int MaxSandboxes = 256;

    private readonly ConcurrentDictionary<Guid, Entry> _sandboxes = new();
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private volatile bool _templateSeeded;
    private long _templateSeededAtTicks;

    /// <summary>
    /// Distinguishes one build of the template from the next in the connection string, so a
    /// rebuild starts from a genuinely empty database instead of joining the shared-cache
    /// in-memory database of the same name that in-flight readers may still be holding open.
    /// </summary>
    private int _templateGeneration;

    private bool TemplateIsFresh =>
        _templateSeeded &&
        DateTimeOffset.UtcNow - new DateTimeOffset(Interlocked.Read(ref _templateSeededAtTicks), TimeSpan.Zero)
            < TemplateLifetime;

    private sealed class Entry(SqliteConnection keepAlive, string connectionString) : IDisposable
    {
        public SqliteConnection KeepAlive { get; } = keepAlive;
        public string ConnectionString { get; } = connectionString;
        private long _lastTouched = DateTimeOffset.UtcNow.UtcTicks;

        public DateTimeOffset LastTouched => new(Interlocked.Read(ref _lastTouched), TimeSpan.Zero);
        public void Touch() => Interlocked.Exchange(ref _lastTouched, DateTimeOffset.UtcNow.UtcTicks);
        public void Dispose() => KeepAlive.Dispose();
    }

    /// <summary>
    /// Builds and seeds the template database if it doesn't exist yet — or builds a fresh one
    /// when the current build has outlived <see cref="TemplateLifetime"/>, so the seeded
    /// history stays anchored to the current date. Safe to call concurrently; every caller
    /// after the first awaits the same build.
    /// </summary>
    public async Task EnsureTemplateAsync(CancellationToken cancellationToken = default)
    {
        if (TemplateIsFresh)
            return;

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (TemplateIsFresh)
                return;

            _sandboxes.TryGetValue(TemplateId, out var previous);
            var entry = OpenDatabase($"demo-template-g{++_templateGeneration}");
            try
            {
                await using (var schemaContext = CreateContext(entry.ConnectionString))
                    await schemaContext.Database.EnsureCreatedAsync(cancellationToken);

                // Registered before seeding: the seeder's scope resolves IAppDbContext
                // through this registry, so the template has to be findable already.
                _sandboxes[TemplateId] = entry;

                using var scope = scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<IDemoSessionLocator>().PinToSandbox(TemplateId);
                await scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>().SeedAsync(cancellationToken);

                _templateSeeded = true;
                Interlocked.Exchange(ref _templateSeededAtTicks, DateTimeOffset.UtcNow.UtcTicks);
                previous?.Dispose();
                logger.LogInformation("Demo template database seeded (generation {Generation})", _templateGeneration);
            }
            catch
            {
                // A failed rebuild keeps the previous build in service: a demo whose history
                // ends a little while ago beats no demo at all. First-time failures leave
                // nothing, and the next caller retries.
                if (previous is not null)
                    _sandboxes[TemplateId] = previous;
                else
                    _sandboxes.TryRemove(TemplateId, out _);
                entry.Dispose();
                throw;
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <summary>Creates a fresh sandbox for a new demo visitor and returns its id.</summary>
    public async Task<Guid> CreateSandboxAsync(CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        await CreateSandboxAsync(id, cancellationToken);
        return id;
    }

    /// <summary>Drops a sandbox, discarding everything its visitor did in it.</summary>
    public void Remove(Guid sandboxId)
    {
        if (sandboxId == TemplateId)
            return;
        if (_sandboxes.TryRemove(sandboxId, out var entry))
            entry.Dispose();
    }

    /// <summary>
    /// Discards everything the visitor did by copying the template back over their sandbox.
    /// Copied in place rather than dropped and rebuilt, because a scope that resolved its context
    /// before the reset keeps using it — a Blazor circuit holds one for the whole visit — and
    /// every query against a sandbox database that has ceased to exist fails. Nothing the visitor
    /// has open is ever pointed at a database that isn't there, and a copy that fails leaves them
    /// the island they had rather than nothing at all.
    /// </summary>
    public async Task ResetSandboxAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
        if (sandboxId == TemplateId)
            return;

        await EnsureTemplateAsync(cancellationToken);

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_sandboxes.TryGetValue(sandboxId, out var entry))
            {
                CopyTemplateInto(entry);
                entry.Touch();
            }
            else
            {
                CreateLocked(sandboxId);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <summary>
    /// Drops the sandboxes nobody has touched for <see cref="IdleTimeout"/>. Called on a slow
    /// loop (see <c>DemoTemplateWarmup</c>) because the only other pass runs when a new visitor
    /// arrives — and the last visitor of the day is exactly the one nobody follows, so without
    /// this their copy would sit in memory until someone else tried the demo.
    /// </summary>
    public async Task EvictIdleAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            EvictStale(makingRoom: false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <summary>
    /// The context a scope's <c>IAppDbContext</c> should be for this sandbox. Synchronous
    /// because it is called from a DI factory; the awkward bridge inside is for the rare path
    /// where a returning visitor's sandbox (or the whole template, after a restart) has to be
    /// rebuilt first.
    /// </summary>
    public DemoAppDbContext CreateContext(Guid sandboxId)
    {
        var entry = Resolve(sandboxId);
        return CreateContext(entry.ConnectionString, entry.Touch);
    }

    private Entry Resolve(Guid sandboxId)
    {
        if (_sandboxes.TryGetValue(sandboxId, out var entry) && (sandboxId == TemplateId || _templateSeeded))
        {
            entry.Touch();
            return entry;
        }

        // Task.Run so blocking never happens on a Blazor circuit's sync context.
        return Task.Run(() => RecreateAsync(sandboxId)).GetAwaiter().GetResult();
    }

    private async Task<Entry> RecreateAsync(Guid sandboxId)
    {
        await EnsureTemplateAsync();
        if (sandboxId == TemplateId)
            return _sandboxes[TemplateId];

        logger.LogInformation("Recreating evicted demo sandbox {SandboxId}", sandboxId);
        return await CreateSandboxAsync(sandboxId);
    }

    private async Task<Entry> CreateSandboxAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureTemplateAsync(cancellationToken);

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_sandboxes.TryGetValue(id, out var existing))
                return existing;

            EvictStale(makingRoom: true);
            return CreateLocked(id);
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <summary>Opens a sandbox database and fills it from the template. Called under the mutex.</summary>
    private Entry CreateLocked(Guid id)
    {
        var entry = OpenDatabase($"demo-sandbox-{id:N}");
        try
        {
            CopyTemplateInto(entry);
        }
        catch
        {
            entry.Dispose();
            throw;
        }

        _sandboxes[id] = entry;
        return entry;
    }

    /// <summary>
    /// Copies the template over a sandbox, replacing whatever was in it — SQLite's backup makes
    /// the destination an exact copy of the source. The keep-alive connections are only ever
    /// used here, under the mutex.
    /// </summary>
    private void CopyTemplateInto(Entry entry)
    {
        using var source = new SqliteConnection(_sandboxes[TemplateId].ConnectionString);
        source.Open();
        source.BackupDatabase(entry.KeepAlive);
    }

    /// <summary>
    /// Drops idle sandboxes, and — when a new one is about to take a slot — the oldest ones
    /// beyond the cap. Called under the mutex.
    /// </summary>
    private void EvictStale(bool makingRoom)
    {
        var now = DateTimeOffset.UtcNow;
        var visitors = _sandboxes.Where(kv => kv.Key != TemplateId).ToList();

        var stale = visitors.Where(kv => now - kv.Value.LastTouched > IdleTimeout).Select(kv => kv.Key);
        var overflow = makingRoom && visitors.Count >= MaxSandboxes
            ? visitors.OrderBy(kv => kv.Value.LastTouched).Take(visitors.Count - MaxSandboxes + 1).Select(kv => kv.Key)
            : Enumerable.Empty<Guid>();

        foreach (var id in stale.Concat(overflow).Distinct().ToList())
        {
            if (_sandboxes.TryRemove(id, out var entry))
            {
                entry.Dispose();
                logger.LogInformation("Evicted demo sandbox {SandboxId}", id);
            }
        }
    }

    private static Entry OpenDatabase(string databaseName)
    {
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";
        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();
        return new Entry(keepAlive, connectionString);
    }

    /// <summary>A context on a sandbox database — always the SQLite-adjusted demo model.</summary>
    private static DemoAppDbContext CreateContext(string connectionString, Action? onUse = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString);
        if (onUse is not null)
            options.AddInterceptors(new SandboxUseInterceptor(onUse));
        return new DemoAppDbContext(options.Options);
    }

    /// <summary>
    /// Reports a sandbox as in use on every database operation, which is what makes the idle
    /// clock in <see cref="EvictStale"/> measure real inactivity. Touching it only where the
    /// context is handed out is not enough: a Blazor circuit resolves its <c>IAppDbContext</c>
    /// once and then keeps it for the whole visit, so a visitor still clicking around after the
    /// idle timeout would have their database evicted from under them — and every query after
    /// that fails on a SQLite database with no tables left in it.
    /// </summary>
    private sealed class SandboxUseInterceptor(Action touch) : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            touch();
            return base.ConnectionOpening(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            touch();
            return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _sandboxes.Values)
            entry.Dispose();
        _sandboxes.Clear();
        _mutex.Dispose();
    }
}
