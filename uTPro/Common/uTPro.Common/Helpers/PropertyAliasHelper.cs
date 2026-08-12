namespace uTPro.Extension
{
    /// <summary>
    /// Utility for converting C# property names (PascalCase) to Umbraco property aliases (camelCase).
    /// <para>
    /// This matches the convention used by ModelsBuilder's <c>[ImplementPropertyType]</c> attribute,
    /// allowing compile-time safety via <c>nameof()</c> instead of hardcoded strings.
    /// </para>
    /// <example>
    /// <code>
    /// // "CategoryItemShowInUrl" → "categoryItemShowInUrl"
    /// var alias = PropertyAliasHelper.ToAlias(nameof(GlobalFolderCategoryItem.CategoryItemShowInUrl));
    /// </code>
    /// </example>
    /// </summary>
    public static class PropertyAliasHelper
    {
        /// <summary>
        /// Converts a PascalCase property name to its camelCase Umbraco property alias.
        /// <summary>
                /// Converts a PascalCase property name to a camelCase alias.
                /// </summary>
                /// <param name="propertyName">The property name to convert.</param>
                /// <returns>The camelCase alias, or the original value if it is null or empty.</returns>
        public static string ToAlias(string propertyName)
            => string.IsNullOrEmpty(propertyName)
                ? propertyName
                : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
