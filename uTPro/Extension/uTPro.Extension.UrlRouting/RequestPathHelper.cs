namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Shared helper for stripping the domain prefix from a request path.
/// </summary>
internal static class RequestPathHelper
{
    /// <summary>
    /// Removes the domain path prefix from <paramref name="decodedPath"/>, ensuring the match
    /// occurs at a segment boundary (i.e. the character immediately after the prefix is '/' or end-of-string).
    /// </summary>
    /// <remarks>
    /// Without the boundary check, a domain path of "vi" would incorrectly match "vietnam/bai-viet"
    /// and produce "etnam/bai-viet".
    /// <summary>
    /// Removes a matching domain path prefix from a request path.
    /// </summary>
    /// <param name="decodedPath">The request path from which to remove the prefix.</param>
    /// <param name="domainPath">The domain path prefix to remove.</param>
    /// <returns>
    /// The trimmed remainder when the prefix matches at a path-segment boundary; otherwise, the original path.
    /// </returns>
    public static string StripDomainPrefix(string decodedPath, string domainPath)
    {
        if (domainPath.Length > 0
            && decodedPath.StartsWith(domainPath, StringComparison.OrdinalIgnoreCase)
            && (decodedPath.Length == domainPath.Length || decodedPath[domainPath.Length] == '/'))
        {
            return decodedPath[domainPath.Length..].Trim('/');
        }

        return decodedPath;
    }
}
