using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Configures ASP.NET Core forwarded-headers processing so the app resolves the real
/// client IP (and scheme) when it runs behind a reverse proxy or load balancer — the
/// typical Linux setup (Kestrel behind nginx/Apache) as well as cloud LBs / Cloudflare.
///
/// Without this, <c>HttpContext.Connection.RemoteIpAddress</c> is the proxy's IP, which
/// silently breaks every per-IP feature — e.g. the SimpleFormBuilder submission rate
/// limiter would throttle all visitors as a single shared partition.
///
/// This concern lives in the host (uTPro), not in the form package: the host owns the
/// deployment topology and therefore decides which proxies to trust. Config section:
/// <c>uTPro:ForwardedHeaders</c>. Disabled by default so direct-exposed Kestrel and
/// IIS in-process deployments (which already surface the real client IP) are unaffected.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string SectionPath = "uTPro:ForwardedHeaders";

    public static IServiceCollection AddForwardedHeadersConfig(
        this IServiceCollection services, WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(SectionPath);
        if (!section.GetValue<bool>("Enabled"))
            return services;

        var knownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var knownNetworks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];
        var trustAllProxies = section.GetValue<bool>("TrustAllProxies");
        var forwardLimit = section.GetValue<int?>("ForwardLimit");

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // ASP.NET Core trusts only loopback out of the box. Clear the defaults so the
            // operator opts into exactly the proxies/networks the app sits behind.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            if (trustAllProxies)
            {
                // Container / dynamic-proxy scenarios where the proxy IP isn't stable.
                // WARNING: only safe when the app is NOT directly reachable by clients.
                // If it is, X-Forwarded-For can be spoofed to bypass per-IP limits.
                options.ForwardLimit = forwardLimit; // null = unlimited hops
                options.KnownNetworks.Add(new IPNetwork(IPAddress.Any, 0));
                options.KnownNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
                return;
            }

            // Number of proxy hops to walk back through X-Forwarded-For. Default 1
            // (single reverse proxy); raise it for chained proxies (e.g. CDN -> nginx).
            options.ForwardLimit = forwardLimit ?? 1;

            foreach (var proxy in knownProxies)
            {
                if (IPAddress.TryParse(proxy, out var ip))
                    options.KnownProxies.Add(ip);
            }

            foreach (var network in knownNetworks)
            {
                if (TryParseNetwork(network, out var net))
                    options.KnownNetworks.Add(net);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers the forwarded-headers middleware at the very front of the pipeline when
    /// enabled, so downstream components (HTTPS redirection, rate limiting, logging) see
    /// the real client IP and scheme. No-op when the feature is disabled.
    /// </summary>
    public static WebApplication UseForwardedHeadersIfEnabled(this WebApplication app)
    {
        if (app.Configuration.GetSection(SectionPath).GetValue<bool>("Enabled"))
            app.UseForwardedHeaders();

        return app;
    }

    /// <summary>Parses a CIDR string (e.g. "10.0.0.0/8", "2001:db8::/32") into an IPNetwork.</summary>
    private static bool TryParseNetwork(string cidr, out IPNetwork network)
    {
        network = default;
        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var prefixLength))
            return false;

        try
        {
            network = new IPNetwork(prefix, prefixLength);
            return true;
        }
        catch (ArgumentException)
        {
            // Invalid prefix length or non-zero host bits for the given prefix.
            return false;
        }
    }
}
