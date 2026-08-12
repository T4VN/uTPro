using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Extension;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Property aliases on <c>globalFolderCategoryItem</c> for URL routing.
/// Derived from the generated model property names via <see cref="PropertyAliasHelper.ToAlias"/> to avoid hardcoded strings.
/// </summary>
public static class CategoryUrlConstants
{
    public static readonly string ShowInUrlAlias = PropertyAliasHelper.ToAlias(nameof(GlobalFolderCategoryItem.CategoryItemShowInUrl));
    public static readonly string UrlSegmentAlias = PropertyAliasHelper.ToAlias(nameof(GlobalFolderCategoryItem.CategoryItemUrlSegment));
    public static readonly string PageCategoriesAlias = PropertyAliasHelper.ToAlias(nameof(GlobalPagePageCategoriesSetting.Categories));
}
