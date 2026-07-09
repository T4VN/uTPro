using Microsoft.AspNetCore.Http;

namespace uTPro.Extension
{
    /// <summary>
    /// Static wrapper around <see cref="IHttpContextAccessor"/> so extension methods
    /// (which have no DI) can participate in per-request caching. Must be initialised
    /// once during startup by resolving <c>IHttpContextAccessor</c> from the container
    /// and assigning it to <see cref="Accessor"/>. Done by
    /// <c>HttpContextStaticComposer</c> in the ContentExtensions startup hook.
    /// </summary>
    public static class HttpContextStatic
    {
        public static IHttpContextAccessor? Accessor { get; set; }
    }
}
