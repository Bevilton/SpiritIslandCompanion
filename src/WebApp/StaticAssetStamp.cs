using Microsoft.Extensions.FileProviders;

namespace WebApp;

/// <summary>
/// Cache-busting stamps for files served straight out of wwwroot.
/// <para>
/// Those files carry no fingerprint and no Cache-Control, so a rebuilt stylesheet or a
/// regenerated script can sit in the browser cache long after the markup has moved on —
/// and ES modules in particular are held onto hard. Appending <c>?v=</c>the write time
/// makes a changed file refetch itself without anyone having to force-reload.
/// </para>
/// <para>
/// Computed on every call, deliberately: the geometry generator rewrites its output under
/// a running dev server, and a stamp cached at first render would keep serving the stale
/// module — the very thing the stamp exists to prevent. The cost is a file stat.
/// </para>
/// </summary>
public static class StaticAssetStamp
{
    /// <summary>
    /// The stamp for a set of wwwroot-relative paths — the newest write time among the ones
    /// that exist, so one stamp can cover a module and the files it imports. With every path
    /// missing it falls back to "now", which stamps a fresh value rather than pretending
    /// nothing changed.
    /// </summary>
    public static string For(IFileProvider webRoot, params string[] paths)
    {
        var newest = paths
            .Select(webRoot.GetFileInfo)
            .Where(f => f.Exists)
            .Select(f => f.LastModified)
            .DefaultIfEmpty(DateTimeOffset.UtcNow)
            .Max();
        return newest.UtcTicks.ToString("x");
    }
}
