using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Community.BlockPreview;
using Umbraco.Community.BlockPreview.Interfaces;
using Umbraco.Community.BlockPreview.Services;

namespace uTPro.Project.Web.Configure
{
    public class CustomBlockPreviewService(
        IRazorViewEngine razorViewEngine,
        IPublishedModelFactory publishedModelFactory,
        BlockEditorConverter blockEditorConverter,
        IOptions<BlockPreviewOptions> options,
        IJsonSerializer jsonSerializer,
        IBlockModelFactory blockModelFactory,
        IBlockViewRenderer blockViewRenderer,
        IBlockDataConverter blockDataConverter,
        IBlockTypeCacheService blockTypeCacheService,
        IBlockPreviewViewResolver viewResolver)
    : BlockPreviewService(publishedModelFactory, blockEditorConverter, options, jsonSerializer, blockModelFactory, blockViewRenderer, blockDataConverter, blockTypeCacheService, viewResolver)
    {
        readonly IRazorViewEngine _razorViewEngine = razorViewEngine;

        protected override ViewEngineResult? GetViewResult(BlockPreviewContext context)
        {
            var blockGrid = CustomPathViews.GetPathViewBlockGridPreview("~/Views/Partials/blockgrid/Components/" + context.ContentAlias, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(blockGrid))
            {
                if (string.IsNullOrEmpty(Path.GetExtension(blockGrid)))
                {
                    return _razorViewEngine.GetView("", viewPath: blockGrid + ".cshtml", isMainPage: false);
                }
                else
                {
                    return _razorViewEngine.GetView("", viewPath: blockGrid, isMainPage: false);
                }
            }
            return base.GetViewResult(context);
        }
    }
}
