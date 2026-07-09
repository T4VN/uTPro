using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using uTPro.Feature.SimpleFormBuilder.Models;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.uTProFormAddon.Turnstile;

/// <summary>
/// Verifies a Cloudflare Turnstile token as part of the SimpleFormBuilder submission pipeline
/// (an <see cref="IFormSubmissionHandler"/>, replacing the earlier PostRouting middleware).
/// Runs after the built-in rate-limit gatekeeper, before the entry is stored.
///
/// This uTPro-specific field type declares its own Site Key / Secret Key settings (see
/// TurnstileComposer), stored in the field's Attributes: the public Site Key is <c>siteKey</c>,
/// the Secret Key is <c>secretKey</c>, and the Failure Message reuses the field's built-in
/// Validation Message. Whatever a field leaves blank falls back to appsettings
/// (<c>uTPro:Feature:Form:Addon:Turnstile</c>), so forms can share global keys or override them.
/// </summary>
public sealed class TurnstileSubmissionHandler(
    IHttpClientFactory httpClientFactory,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileSubmissionHandler> logger) : IFormSubmissionHandler
{
    private const string FieldType = "turnstile";
    private const string DefaultFailureMessage = "Captcha verification failed. Please try again.";
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // After the rate-limit gatekeeper (int.MinValue), before any higher-order handlers.
    public int Order => 100;

    public async Task<FormSubmissionResult> HandleAsync(FormSubmissionContext context, CancellationToken cancellationToken)
    {
        var opts = options.Value;

        foreach (var field in EnumerateFields(context.Form).Where(f => f.Type == FieldType))
        {
            // Secret Key: field value (secretKey attribute) first, then appsettings fallback.
            var secret = FirstNonEmpty(GetAttr(field, "secretKey"), opts.SecretKey);
            if (string.IsNullOrWhiteSpace(secret)) continue; // display-only / not configured

            context.Data.TryGetValue(field.Name, out var token);

            if (!await VerifyAsync(secret!, token, context.IpAddress, cancellationToken))
            {
                var message = FirstNonEmpty(field.ValidationMessage, opts.FailureMessage)
                    ?? DefaultFailureMessage;
                return FormSubmissionResult.Reject(message);
            }
        }

        return FormSubmissionResult.Continue;
    }

    private static string? FirstNonEmpty(string? primary, string? fallback)
        => !string.IsNullOrWhiteSpace(primary) ? primary : (string.IsNullOrWhiteSpace(fallback) ? null : fallback);

    private static string? GetAttr(FormFieldViewModel field, string key)
        => field.Attributes is not null && field.Attributes.TryGetValue(key, out var v)
            ? v
            : null;

    private static IEnumerable<FormFieldViewModel> EnumerateFields(FormViewModel form)
    {
        foreach (var f in form.Fields) yield return f;
        foreach (var g in form.Groups)
            foreach (var c in g.Columns)
                foreach (var f in c.Fields)
                    yield return f;
    }

    private async Task<bool> VerifyAsync(
        string secretKey, string? token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new("secret", secretKey),
                new("response", token),
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
                fields.Add(new("remoteip", remoteIp));

            var client = httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(fields);
            using var response = await client.PostAsync(VerifyUrl, content, ct);
            if (!response.IsSuccessStatusCode) return false;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.True;
        }
        catch (Exception ex)
        {
            // Fail closed: a broken verification call must not let a submission through.
            logger.LogWarning(ex, "Turnstile verification request failed.");
            return false;
        }
    }
}
