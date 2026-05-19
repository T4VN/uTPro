using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace uTPro.Extension;

/// <summary>
/// Allows block components to register JS file paths into a per-request queue.
/// The Layout then reads the queue and appends them to the deferred loader array
/// (after jQuery) or renders them as standalone defer scripts.
///
/// Usage in component:
///   Html.QueueScript("/scripts/uTPro/slider.js");           // needs jQuery
///   Html.QueueStandaloneScript("/scripts/uTPro/cookie.js"); // no dependencies
///
/// Usage in Layout:
///   var scripts = [ ... @Html.RenderScriptQueue() ];        // appends to deferred array
///   @Html.RenderStandaloneScripts()                         // renders defer script tags
/// </summary>
public static class ScriptQueueExtensions
{
    private static readonly object _deferredKey = new();
    private static readonly object _standaloneKey = new();

    /// <summary>
    /// Register a script that depends on jQuery (or other deferred libraries).
    /// It will be appended to the sequential deferred loader in _Layout.
    /// Duplicates are ignored automatically.
    /// </summary>
    public static void QueueScript(this IHtmlHelper helper, string path)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var queue = GetQueue(helper.ViewContext.HttpContext, _deferredKey, create: true)!;
        queue.Add(path);
    }

    /// <summary>
    /// Register a standalone script (no jQuery dependency).
    /// It will be rendered as a separate &lt;script defer&gt; tag.
    /// Duplicates are ignored automatically.
    /// </summary>
    public static void QueueStandaloneScript(this IHtmlHelper helper, string path)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var queue = GetQueue(helper.ViewContext.HttpContext, _standaloneKey, create: true)!;
        queue.Add(path);
    }

    /// <summary>
    /// Renders queued deferred scripts as comma-prefixed JS string literals
    /// to be appended to the existing scripts array in the deferred loader.
    /// Returns empty if no scripts were queued.
    /// <para>Example output: <c>,'/scripts/uTPro/slider.js','/scripts/uTPro/lightbox.js'</c></para>
    /// </summary>
    public static IHtmlContent RenderScriptQueue(this IHtmlHelper helper)
    {
        ArgumentNullException.ThrowIfNull(helper);
        var queue = GetQueue(helper.ViewContext.HttpContext, _deferredKey, create: false);
        if (queue == null || queue.Count == 0)
            return HtmlString.Empty;

        var entries = string.Join(",", queue.Select(p => $"'{System.Web.HttpUtility.JavaScriptStringEncode(p)}'"));
        return new HtmlString("," + entries);
    }

    /// <summary>
    /// Renders standalone script tags with defer attribute.
    /// Returns empty if no standalone scripts were queued.
    /// </summary>
    public static IHtmlContent RenderStandaloneScripts(this IHtmlHelper helper)
    {
        ArgumentNullException.ThrowIfNull(helper);
        var queue = GetQueue(helper.ViewContext.HttpContext, _standaloneKey, create: false);
        if (queue == null || queue.Count == 0)
            return HtmlString.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var path in queue)
        {
            var encoded = System.Web.HttpUtility.HtmlAttributeEncode(path);
            sb.Append($"<script src=\"{encoded}\" defer></script>\n");
        }
        return new HtmlString(sb.ToString());
    }

    private static HashSet<string>? GetQueue(HttpContext? http, object key, bool create)
    {
        if (http == null) return null;
        if (http.Items.TryGetValue(key, out var existing) && existing is HashSet<string> set)
            return set;
        if (!create) return null;
        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        http.Items[key] = set;
        return set;
    }
}
