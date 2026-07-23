using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace uTPro.Extension
{
    /// <summary>
    /// Resolves the "content layout" preset for a page (full / left-sidebar / right-sidebar).
    ///
    /// <para>Resolution order (see the child-page-layout feature):
    /// the page's own <c>layout</c> property → the nearest <c>globalFolderPages</c> container's
    /// <c>defaultLayout</c> property → the system default <c>full</c>. Any value that is empty or
    /// not one of the three known presets is treated as "not set" and falls through.</para>
    ///
    /// <para>Properties are read by alias so this works before/after ModelsBuilder regenerates the
    /// strongly-typed models, and degrades to <c>full</c> if the properties don't exist yet.</para>
    /// </summary>
    public static class LayoutExtension
    {
        public const string LayoutFull = "full";
        public const string LayoutLeftSidebar = "left-sidebar";
        public const string LayoutRightSidebar = "right-sidebar";

        // Container document type that holds the child pages (see HiddenContainerAliases).
        private const string PagesContainerAlias = "globalFolderPages";
        private const string LayoutPropertyAlias = "layout";
        private const string DefaultLayoutPropertyAlias = "defaultLayout";

        // The datatype also offers an explicit "inherit" option so editors can revert a
        // RadioButtonList selection (which cannot be unselected in the UI). "inherit" is
        // intentionally NOT a known layout, so Normalize() returns null for it and the
        // resolver falls through to the container default / "full".
        private static readonly string[] KnownLayouts = { LayoutFull, LayoutLeftSidebar, LayoutRightSidebar };

        /// <summary>Effective layout preset for the page. Never returns null; defaults to <c>full</c>.</summary>
        public static string GetContentLayout(this IPublishedContent content)
        {
            // 1) The page's own selection.
            var own = Normalize(content.Value<string>(LayoutPropertyAlias));
            if (own != null)
            {
                return own;
            }

            // 2) The default configured on the nearest Pages container ancestor.
            var container = content.Ancestors()
                .FirstOrDefault(a => a.ContentType.Alias.Equals(PagesContainerAlias, StringComparison.OrdinalIgnoreCase));

            var fromContainer = container is null
                ? null
                : Normalize(container.Value<string>(DefaultLayoutPropertyAlias));

            if (fromContainer != null)
            {
                return fromContainer;
            }

            // 3) System default.
            return LayoutFull;
        }

        /// <summary>True when the layout renders a sidebar column beside the main content.</summary>
        public static bool HasSidebar(this string? layout)
            => layout == LayoutLeftSidebar || layout == LayoutRightSidebar;

        /// <summary>
        /// Resolves an inherited block-grid region (e.g. <c>topComponent</c>) as an ordered list of
        /// block grids to render (outermost first).
        ///
        /// <para>By default the value from the nearest ancestor that defines it is used (override).
        /// If that owner node has the <paramref name="appendFlagAlias"/> toggle ON, the region
        /// inherited from further-out ancestors is ALSO included (append), so e.g. a container can
        /// add a breadcrumb on top of the site-wide header instead of replacing it.</para>
        /// </summary>
        public static IReadOnlyList<BlockGridModel> GetInheritedRegion(
            this IPublishedContent content, string regionAlias, string appendFlagAlias)
        {
            var models = new List<BlockGridModel>();

            // Inherited() returns the nearest node (self first) that has a value for the alias.
            var owner = content.Inherited(regionAlias);
            while (owner is not null)
            {
                var model = owner.Value<BlockGridModel>(regionAlias);
                if (model is not null && model.Any())
                {
                    // Outer levels are discovered later, so insert at the front to keep them on top.
                    models.Insert(0, model);
                }

                // Only keep walking outwards when this owner opts to append its inherited region.
                if (!owner.Value<bool>(appendFlagAlias))
                {
                    break;
                }

                owner = owner.Parent?.Inherited(regionAlias);
            }

            return models;
        }

        // Returns one of the known layout values (case-insensitive match) or null when unset/invalid.
        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();

            foreach (var known in KnownLayouts)
            {
                if (string.Equals(known, value, StringComparison.OrdinalIgnoreCase))
                {
                    return known;
                }
            }

            return null;
        }
    }
}
