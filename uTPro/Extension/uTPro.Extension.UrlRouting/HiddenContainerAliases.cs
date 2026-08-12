using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Options for <see cref="HiddenUrlProvider"/>. Bound from the <c>uTPro:HiddenUrls</c> section.
/// </summary>
public sealed class HiddenUrlOptions
{
    public const string SectionName = "uTPro:HiddenUrls";

    /// <summary>Document type aliases whose nodes should not generate a URL.</summary>
    public string[] DocumentTypeAliases { get; set; } = [];
}

/// <summary>
/// Resolved set of "container" document type aliases (built-in defaults + configured extras).
/// Shared by <see cref="HiddenUrlProvider"/> (suppresses the container's own URL) and the
/// transparent-routing classes, so both features stay in sync from a single source of truth.
/// </summary>
public sealed class HiddenContainerAliases
{
    /// <summary>
    /// Built-in container document types that are always hidden.
    /// </summary>
    private static readonly string[] DefaultDocumentTypeAliases =
    [
        GlobalFolderRoot.ModelTypeAlias,
        GlobalFolderSites.ModelTypeAlias,
        GlobalFolderNavigationLinkForSite.ModelTypeAlias,
        GlobalFolderSettings.ModelTypeAlias,
        GlobalFolderPages.ModelTypeAlias,
        GlobalFolderCategories.ModelTypeAlias,
        GlobalFolderCategoryGroup.ModelTypeAlias,
        GlobalFolderCategoryItem.ModelTypeAlias,
    ];

    /// <summary>
    /// Initializes the configured hidden container aliases.
    /// </summary>
    /// <param name="options">The options containing additional document type aliases.</param>
    public HiddenContainerAliases(IOptions<HiddenUrlOptions> options)
    {
        var set = new HashSet<string>(DefaultDocumentTypeAliases, StringComparer.OrdinalIgnoreCase);

        foreach (var alias in options.Value.DocumentTypeAliases ?? [])
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                set.Add(alias);
            }
        }

        Aliases = set;
    }

    /// <summary>The resolved, case-insensitive set of hidden container aliases.</summary>
    public IReadOnlySet<string> Aliases { get; }

    /// <summary>
/// Determines whether a document type alias identifies a hidden container.
/// </summary>
/// <param name="alias">The document type alias to check.</param>
/// <returns><c>true</c> if the alias identifies a hidden container; otherwise, <c>false</c>.</returns>
    public bool Contains(string? alias) => alias is not null && Aliases.Contains(alias);

    /// <summary>
    /// The only container whose transparency is per-node (toggled by editors).
    /// </summary>
    public static readonly string TogglableContainerAlias = GlobalFolderPages.ModelTypeAlias;

    /// <summary>Boolean property alias on the togglable container that, when ON, makes the
    /// container's segment appear in child URLs.</summary>
    public const string ShowInUrlPropertyAlias = "showInUrl";

    /// <summary>
        /// Determines whether the container should be shown in the URL.
        /// </summary>
        /// <param name="alias">The container alias.</param>
        /// <param name="showInUrl">Whether the container is configured to appear in the URL.</param>
        /// <returns><c>true</c> if the alias identifies the togglable container and it is enabled for URL display; otherwise, <c>false</c>.</returns>
        private bool IsShown(string alias, bool showInUrl)
        => alias.Equals(TogglableContainerAlias, StringComparison.OrdinalIgnoreCase) && showInUrl;

    /// <summary>
    /// True when the node should be treated as transparent (dropped from public URLs).
    /// <summary>
    /// Determines whether a published content node should have a transparent URL.
    /// </summary>
    /// <param name="node">The published content node to evaluate.</param>
    /// <returns><c>true</c> if the node is configured as a hidden container and should be transparent; otherwise, <c>false</c>.</returns>
    public bool IsTransparent(IPublishedContent? node)
    {
        if (node is null || !Aliases.Contains(node.ContentType.Alias))
        {
            return false;
        }

        return !IsShown(node.ContentType.Alias, node.Value<bool>(ShowInUrlPropertyAlias));
    }

    /// <inheritdoc cref="IsTransparent(IPublishedContent?)"/>
    public bool IsTransparent(Umbraco.Cms.Core.Models.IContent? node)
    {
        if (node is null || !Aliases.Contains(node.ContentType.Alias))
        {
            return false;
        }

        return !IsShown(node.ContentType.Alias, node.GetValue<bool>(ShowInUrlPropertyAlias));
    }
}
