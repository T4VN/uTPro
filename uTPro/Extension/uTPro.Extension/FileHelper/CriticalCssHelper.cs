using System.Collections.Concurrent;
using uTPro.Common.Constants;

namespace uTPro.Extension.FileHelper
{
    /// <summary>
    /// Reads CSS files from disk once and caches content in memory for inline rendering.
    /// This gives you the maintainability of editing standalone .css files while still
    /// outputting inline &lt;style&gt; in HTML (no extra HTTP round-trip for critical CSS).
    /// Supports multiple files — each path is cached independently.
    /// </summary>
    public static class CriticalCssHelper
    {
        private static readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the content of a CSS file from the web root.
        /// The file is read once and cached for the lifetime of the application.
        /// </summary>
        /// <param name="relativePath">
        /// Path relative to wwwroot, e.g. <c>css/uTPro/critical.css</c>.
        /// Forward slashes are normalised to the platform directory separator.
        /// </param>
        public static string Get(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }
            return _cache.GetOrAdd(relativePath, path =>
            {
                var webRoot = PathFolder.DirectoryWWWRoot;
                var fullPath = Path.Combine(webRoot, path.Replace('/', Path.DirectorySeparatorChar));
                return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
            });
        }

        /// <summary>
        /// Clears all cached entries. Useful for dev hot-reload.
        /// </summary>
        public static void Invalidate() => _cache.Clear();
    }
}
