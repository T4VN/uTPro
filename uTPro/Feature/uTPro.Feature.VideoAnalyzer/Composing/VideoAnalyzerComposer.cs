using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;
using uTPro.Feature.VideoAnalyzer.Configuration;
using uTPro.Feature.VideoAnalyzer.Migrations;
using uTPro.Feature.VideoAnalyzer.Providers;
using uTPro.Feature.VideoAnalyzer.Services;

namespace uTPro.Feature.VideoAnalyzer.Composing;

/// <summary>
/// Wires up the Video Analyzer feature: options, HttpClients (long timeout for the
/// video-watching Gemini pass), its own tables via an Umbraco migration plan, the
/// analysis pipeline, and the member groups the tool is gated by.
/// </summary>
public sealed class VideoAnalyzerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<VideoAnalyzerOptions>(
            builder.Config.GetSection(VideoAnalyzerOptions.SectionPath));

        builder.Services.AddHttpClient(YouTubeHttpClientNames.Gemini, client =>
        {
            // Watching a long video server-side can legitimately take minutes.
            client.Timeout = TimeSpan.FromMinutes(6);
        });
        builder.Services.AddHttpClient(YouTubeHttpClientNames.YouTubeData, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddSingleton<IVideoAnalyzerRepository, VideoAnalyzerRepository>();
        builder.Services.AddSingleton<IQuotaService, QuotaService>();
        builder.Services.AddSingleton<IAnalysisPipeline, AnalysisPipeline>();
        builder.Services.AddSingleton<IYouTubeMetadataProvider, YouTubeDataApiProvider>();
        builder.Services.AddSingleton<ITranscriptProvider, TranscriptProvider>();
        builder.Services.AddSingleton<IGeminiAnalyzer, GeminiAnalyzerClient>();

        builder.PackageMigrationPlans().Add<VideoAnalyzerMigrationPlan>();

        builder.AddNotificationHandler<Umbraco.Cms.Core.Notifications.UmbracoApplicationStartingNotification,
            MemberGroupsStartupHandler>();
    }
}

/// <summary>Shared HttpClient names (kept here because both sides need the same strings).</summary>
public static class YouTubeHttpClientNames
{
    public const string Gemini = "uTProVideoAnalyzerGemini";
    public const string YouTubeData = "uTProVideoAnalyzerYouTubeData";
}
