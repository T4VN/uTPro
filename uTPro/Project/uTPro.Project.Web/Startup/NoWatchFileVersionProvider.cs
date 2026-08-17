using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// A file version provider that computes content hashes without creating inotify file watchers.
/// Replaces the default <see cref="DefaultFileVersionProvider"/> which calls Watch() on every
/// file path it versions (via asp-append-version), exhausting inotify instances on constrained
/// hosts like Render Free (128 inotify limit).
///
/// Trade-off: file version strings are computed once and cached in-memory for the lifetime
/// of the application. A restart is needed to pick up new static file versions. This is
/// acceptable in production where files don't change at runtime.
/// </summary>
internal sealed class NoWatchFileVersionProvider : IFileVersionProvider
{
    private readonly IWebHostEnvironment _env;
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public NoWatchFileVersionProvider(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string AddFileVersionToPath(PathString requestPathBase, string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var resolvedPath = path;

        // Strip query string if present
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
            resolvedPath = path[..queryIndex];

        lock (_lock)
        {
            if (_cache.TryGetValue(resolvedPath, out var cached))
                return cached;
        }

        var versionedPath = ResolveVersionedPath(requestPathBase, path, resolvedPath);

        lock (_lock)
        {
            _cache[resolvedPath] = versionedPath;
        }

        return versionedPath;
    }

    private string ResolveVersionedPath(PathString requestPathBase, string originalPath, string resolvedPath)
    {
        // Try to find the file relative to web root
        var filePath = resolvedPath;
        if (filePath.StartsWith('/'))
            filePath = filePath[1..];

        // Strip the request path base if present
        if (requestPathBase.HasValue && resolvedPath.StartsWith(requestPathBase.Value, StringComparison.OrdinalIgnoreCase))
        {
            filePath = resolvedPath[requestPathBase.Value.Length..];
            if (filePath.StartsWith('/'))
                filePath = filePath[1..];
        }

        var fileInfo = _env.WebRootFileProvider.GetFileInfo(filePath);
        if (!fileInfo.Exists || fileInfo.IsDirectory)
            return originalPath;

        try
        {
            using var stream = fileInfo.CreateReadStream();
            var hash = SHA256.HashData(stream);
            var version = Convert.ToBase64String(hash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            var separator = originalPath.Contains('?') ? "&" : "?";
            return $"{originalPath}{separator}v={version}";
        }
        catch
        {
            // If hashing fails (file locked, etc.), return the path unversioned.
            return originalPath;
        }
    }
}
