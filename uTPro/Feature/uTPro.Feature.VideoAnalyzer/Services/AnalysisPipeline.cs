using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using uTPro.Feature.VideoAnalyzer.Configuration;
using uTPro.Feature.VideoAnalyzer.Models;
using uTPro.Feature.VideoAnalyzer.Persistence;
using uTPro.Feature.VideoAnalyzer.Providers;

namespace uTPro.Feature.VideoAnalyzer.Services;

/// <summary>
/// Runs one analysis end-to-end in the background: metadata → transcript (best effort) →
/// Gemini (video-URL pass, transcript fallback) → report persisted as JSON.
/// In-process on purpose: free-tier hosts kill background workers between requests, so the
/// state machine lives in the database and recovers stale rows on the next poll.
/// </summary>
public interface IAnalysisPipeline
{
    Task StartAsync(VaVideoAnalysis entity);

    /// <summary>Marks long-unseen Processing rows as failed so they can be retried (quota-free).</summary>
    Task RecoverStaleJobsAsync();
}

internal sealed class AnalysisPipeline(
    IVideoAnalyzerRepository repository,
    IYouTubeMetadataProvider metadataProvider,
    ITranscriptProvider transcriptProvider,
    IGeminiAnalyzer gemini,
    IOptions<VideoAnalyzerOptions> options,
    ILogger<AnalysisPipeline> logger) : IAnalysisPipeline
{
    private static readonly string[] ValidAgeLabels = ["P", "K", "C13", "C16", "C18"];

    // One writer per video: second submissions for the same video join the active row instead.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> VideoLocks = new(StringComparer.Ordinal);

    private static long _lastRecoveryTicks;

    private static readonly JsonSerializerOptions JsonOptions = new();

    public async Task StartAsync(VaVideoAnalysis entity)
    {
        _ = Task.Run(() => RunAsync(entity));
        await Task.CompletedTask;
    }

    public async Task RecoverStaleJobsAsync()
    {
        // At most once per minute per instance.
        var nowTicks = DateTime.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastRecoveryTicks);
        if (nowTicks - last < TimeSpan.FromMinutes(1).Ticks)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastRecoveryTicks, nowTicks, last) != last)
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddMinutes(-options.Value.StaleJobMinutes);
        var stale = await repository.GetStaleProcessingAsync(cutoff);
        foreach (var job in stale)
        {
            logger.LogWarning("VideoAnalyzer: recovering stale job {Key} (video {VideoId}).", job.Key, job.VideoId);
            job.Status = (int)AnalysisStatus.Failed;
            job.Stage = null;
            job.ErrorMessage = "Analysis timed out — please try again.";
            job.CompletedAt = DateTime.UtcNow;
            await repository.UpdateAsync(job);
        }
    }

    private async Task RunAsync(VaVideoAnalysis entity)
    {
        var lockKey = $"{entity.Platform}:{entity.VideoId}";
        var gate = VideoLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            using var heartbeat = StartHeartbeat(entity.Id);
            await RunInternalAsync(entity);
        }
        catch (OperationCanceledException)
        {
            await MarkFailedAsync(entity, "Analysis was cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VideoAnalyzer: analysis failed for video {VideoId}.", entity.VideoId);
            await MarkFailedAsync(entity, ex.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task RunInternalAsync(VaVideoAnalysis entity)
    {
        entity.Status = (int)AnalysisStatus.Processing;
        entity.HeartbeatAt = DateTime.UtcNow;
        await repository.UpdateAsync(entity);

        // 1. Official metadata (Data API), community-scrape fallback for the basics.
        entity.Progress = 15;
        entity.Stage = "metadata";
        await repository.UpdateAsync(entity);

        var metadata = await metadataProvider.GetVideoAsync(entity.VideoId)
            ?? await transcriptProvider.GetBasicInfoAsync(entity.VideoId);
        if (metadata is null)
        {
            throw new InvalidOperationException(
                "Could not fetch video info — the video may be private, deleted, age-restricted, or the API keys are not configured.");
        }

        entity.Title = metadata.Title;
        entity.ChannelTitle = metadata.ChannelTitle;
        entity.ThumbnailUrl = metadata.ThumbnailUrl;
        entity.DurationSeconds = metadata.DurationSeconds;
        entity.MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions);

        if (options.Value.MaxVideoMinutes > 0
            && metadata.DurationSeconds > options.Value.MaxVideoMinutes * 60)
        {
            throw new InvalidOperationException(
                $"This video is {metadata.DurationSeconds / 60} minutes long — the limit is {options.Value.MaxVideoMinutes} minutes.");
        }

        await repository.UpdateAsync(entity);

        // 2. Transcript — best effort, never fatal.
        entity.Progress = 35;
        entity.Stage = "transcript";
        await repository.UpdateAsync(entity);

        var segments = await transcriptProvider.GetTranscriptAsync(entity.VideoId, CancellationToken.None);
        if (segments is { Count: > 0 })
        {
            entity.TranscriptJson = JsonSerializer.Serialize(segments, JsonOptions);
            await repository.UpdateAsync(entity);
        }

        // 3. Gemini analysis: watch the video directly; fall back to transcript text.
        entity.Progress = 60;
        entity.Stage = "analysis";
        await repository.UpdateAsync(entity);

        var metadataContext = BuildMetadataContext(metadata);
        VideoReport report;
        try
        {
            report = await gemini.AnalyzeVideoUrlAsync(
                entity.VideoId, entity.ReportLanguage ?? "vi", metadataContext, CancellationToken.None);
        }
        catch (GeminiQuotaException)
        {
            throw; // quota deserves a distinct message — do not silently burn transcript calls.
        }
        catch (Exception ex) when (segments is { Count: > 0 })
        {
            logger.LogWarning(ex, "VideoAnalyzer: video-URL pass failed for {VideoId}, using transcript fallback.",
                entity.VideoId);
            report = await gemini.AnalyzeTranscriptAsync(
                BuildTranscriptText(segments), metadataContext, entity.ReportLanguage ?? "vi", CancellationToken.None);
        }

        if (!ValidAgeLabels.Contains(report.AgeRating.Label, StringComparer.OrdinalIgnoreCase))
        {
            report.AgeRating.Label = "K";
        }

        // 4. Persist the report.
        entity.Progress = 95;
        entity.Stage = "saving";
        await repository.UpdateAsync(entity);

        entity.ReportJson = JsonSerializer.Serialize(report, JsonOptions);
        entity.Status = (int)AnalysisStatus.Success;
        entity.CompletedAt = DateTime.UtcNow;
        entity.Stage = null;
        await repository.UpdateAsync(entity);
    }

    /// <summary>Refreshes HeartbeatAt every 30s while the analysis runs; dispose to stop.</summary>
    private IDisposable StartHeartbeat(int id)
    {
        var cts = new CancellationTokenSource();
        var task = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    try
                    {
                        await repository.TouchHeartbeatAsync(id, DateTime.UtcNow);
                    }
                    catch (Exception ex)
                    {
                        // Transient DB hiccup — the next tick retries.
                        logger.LogWarning(ex, "VideoAnalyzer: heartbeat update failed for analysis {Id}.", id);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        });

        return new HeartbeatHandle(cts, task);
    }

    private sealed class HeartbeatHandle(CancellationTokenSource cts, Task task) : IDisposable
    {
        public void Dispose()
        {
            cts.Cancel();
            try
            {
                task.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore teardown races — the timer loop exits on cancel.
            }

            cts.Dispose();
        }
    }

    private async Task MarkFailedAsync(VaVideoAnalysis entity, string message)
    {
        entity.Status = (int)AnalysisStatus.Failed;
        entity.Stage = null;
        entity.ErrorMessage = message.Length > 1000 ? message[..1000] : message;
        entity.CompletedAt = DateTime.UtcNow;
        try
        {
            await repository.UpdateAsync(entity);
        }
        catch (Exception ex)
        {
            // Final failure bookkeeping must not throw out of the background task.
            logger.LogError(ex, "VideoAnalyzer: could not persist failure state for {Key}.", entity.Key);
        }
    }

    private static string BuildMetadataContext(VideoMetadata metadata)
    {
        var sb = new StringBuilder();
        sb.AppendLine("VIDEO METADATA (from the official YouTube Data API):");
        sb.AppendLine($"- Title: {metadata.Title}");
        sb.AppendLine($"- Channel: {metadata.ChannelTitle}");
        sb.AppendLine($"- Duration: {metadata.DurationSeconds / 60}m{metadata.DurationSeconds % 60:D2}s");
        sb.AppendLine($"- Views: {metadata.ViewCount}");
        if (metadata.PublishedAt != default)
        {
            sb.AppendLine($"- Published: {metadata.PublishedAt:yyyy-MM-dd}");
        }

        if (metadata.Tags.Count > 0)
        {
            sb.AppendLine($"- Tags: {string.Join(", ", metadata.Tags.Take(30))}");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            var description = metadata.Description.Length <= 3000
                ? metadata.Description
                : metadata.Description[..3000] + "…";
            sb.AppendLine($"- Description:\n{description}");
        }

        return sb.ToString();
    }

    private static string BuildTranscriptText(List<TranscriptSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var segment in segments)
        {
            var seconds = (int)segment.S;
            sb.Append($"{seconds / 60:D2}:{seconds % 60:D2} ").AppendLine(segment.T);
        }

        return sb.ToString();
    }
}
