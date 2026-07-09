using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using uTPro.Feature.SimpleFormBuilder.Models;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.uTProFormAddon.Turnstile;

/// <summary>
/// Verifies a Cloudflare Turnstile token on the SimpleFormBuilder submit endpoint
/// (<c>POST /api/utpro/simple-form/submit</c>). This uTPro-specific field type declares its own
/// Site Key / Secret Key settings (see TurnstileComposer), stored in the field's Attributes:
/// the public Site Key is <c>siteKey</c>, the Secret Key is <c>secretKey</c>, and the Failure
/// Message reuses the field's built-in Validation Message. Whatever a field leaves blank falls
/// back to appsettings (<c>uTProFormAddon:Turnstile</c>), so forms can share global keys or
/// override them individually.
/// </summary>
public sealed class TurnstileValidationMiddleware(RequestDelegate next)
{
    private const string SubmitPath = "/api/utpro/simple-form/submit";
    private const string FieldType = "turnstile";
    private const string DefaultFailureMessage = "Captcha verification failed. Please try again.";
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task InvokeAsync(
        HttpContext context,
        IuTProSimpleFormService formService,
        IHttpClientFactory httpClientFactory,
        IOptions<TurnstileOptions> options,
        ILogger<TurnstileValidationMiddleware> logger)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.Equals(SubmitPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var (alias, data) = await ReadSubmissionAsync(context.Request);

        // No alias / can't parse → let the endpoint deal with it (it returns its own 400).
        if (!string.IsNullOrEmpty(alias))
        {
            var form = formService.GetFormByAlias(alias);
            if (form != null)
            {
                var opts = options.Value;
                foreach (var field in EnumerateFields(form).Where(f => f.Type == FieldType))
                {
                    // Secret Key: field value (secretKey attribute) first, then appsettings fallback.
                    var secret = FirstNonEmpty(GetAttr(field, "secretKey"), opts.SecretKey);
                    if (string.IsNullOrWhiteSpace(secret)) continue; // display-only / not configured

                    data.TryGetValue(field.Name, out var token);
                    var ip = context.Connection.RemoteIpAddress?.ToString();

                    if (!await VerifyAsync(httpClientFactory, secret!, token, ip, context.RequestAborted, logger))
                    {
                        var message = FirstNonEmpty(field.ValidationMessage, opts.FailureMessage)
                            ?? DefaultFailureMessage;

                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(new { message }), context.RequestAborted);
                        return;
                    }
                }
            }
        }

        await next(context);
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

    /// <summary>
    /// Extracts the form alias and submitted values from either a multipart/form-data body
    /// (alias field + JSON "data" field) or a raw JSON body. The JSON body is buffered and
    /// rewound so the downstream controller can still read it.
    /// </summary>
    private static async Task<(string? Alias, Dictionary<string, string> Data)> ReadSubmissionAsync(HttpRequest request)
    {
        try
        {
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(); // cached; controller re-reads safely
                var alias = form["alias"].ToString();
                var dataJson = form["data"].ToString();
                var data = string.IsNullOrEmpty(dataJson)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(dataJson, JsonOpts) ?? new();
                return (alias, data);
            }

            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body)) return (null, new());

            var req = JsonSerializer.Deserialize<SubmitFormRequest>(body, JsonOpts);
            return (req?.Alias, req?.Data ?? new());
        }
        catch
        {
            return (null, new());
        }
    }

    private static async Task<bool> VerifyAsync(
        IHttpClientFactory httpClientFactory, string secretKey, string? token,
        string? remoteIp, CancellationToken ct, ILogger logger)
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
