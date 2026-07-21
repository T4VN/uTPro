using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.UmbracoContext;
using static Umbraco.Cms.Core.Constants.Conventions;

namespace uTPro.Extension.CurrentSite
{
    internal class CurrentSiteExtension : ICurrentSiteExtension
    {
        private readonly ILogger<CurrentSiteExtension> _logger;
        private readonly ICultureDictionary _cultureDictionary;
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        // Lazy resolution breaks the circular dependency:
        // CurrentSiteExtension -> ICurrentItemExtension -> ICurrentSiteExtension
        private readonly Lazy<ICurrentItemExtension> _lazyItem;

        public CurrentSiteExtension(
            ILogger<CurrentSiteExtension> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IUmbracoContextFactory umbracoContextFactory,
            ICultureDictionary cultureDictionary,
            IServiceProvider serviceProvider)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _umbracoContextFactory = umbracoContextFactory;
            _cultureDictionary = cultureDictionary;
            _logger = logger;

            // Resolve lazily from the same scope (scoped service → same scope → same instance).
            _lazyItem = new Lazy<ICurrentItemExtension>(
                () => serviceProvider.GetRequiredService<ICurrentItemExtension>());
        }

        public string DefaultCulture
        {
            get => UContext.Domains?.DefaultCulture ?? Thread.CurrentThread.CurrentCulture.Name;
        }

        private CultureInfo? _currentCulture;
        public CultureInfo CurrentCulture
        {
            get
            {
                _currentCulture ??= Thread.CurrentThread.CurrentCulture;
                return _currentCulture;
            }
        }

        public string CurrentPage => UContext.OriginalRequestUrl.ToString();

        public IWebHostEnvironment WebHostEnvironment => _webHostEnvironment;

        public IConfiguration Configuration => _configuration;

        /// <summary>
        /// Returns the memoized <see cref="ICurrentItemExtension"/> for this request.
        /// No new scope is created — the instance lives in the same DI scope as this service.
        /// </summary>
        public ICurrentItemExtension GetItem() => _lazyItem.Value;

        private IUmbracoContext? _umbracoContext;
        public IUmbracoContext UContext
        {
            get
            {
                _umbracoContext ??= _umbracoContextFactory.EnsureUmbracoContext().UmbracoContext;
                return _umbracoContext ?? throw new Exception("UmbracoContext is null");
            }
        }

        public IEnumerable<PublishedCultureInfo> GetCultures()
        {
            var culs = GetItem().PageHome?.Cultures;
            if (culs == null || culs.Count == 1)
            {
                yield return new PublishedCultureInfo(DefaultCulture, DefaultCulture, null, DateTime.Now);
            }
            else
            {
                foreach (var item in culs)
                {
                    yield return item.Value;
                }
            }
        }

        public string GetDictionaryValue(string key, string valueDefault = "", bool showKey = false)
        {
            key = GetItem().Root.Name + "." + key;
            var dictionaryValue = _cultureDictionary[key];
            if (string.IsNullOrWhiteSpace(dictionaryValue))
            {
                dictionaryValue = showKey ? key : valueDefault;
            }
            return dictionaryValue;
        }

        // Simple in-memory cache for all domains to reduce repeated UContext access.
        // Stored as IReadOnlyList to avoid re-enumeration on .Any() checks.
        private static readonly Lock _domainsLock = new();
        private static IReadOnlyList<Domain> _cachedAllDomains = [];
        private static DateTime _cachedAllDomainsExpires = DateTime.MinValue;
        private const int CachedAllDomainsSeconds = 60;

        public IReadOnlyList<Domain> GetDomains(bool isGetAll)
        {
            if (isGetAll)
            {
                // Return cached list when available
                if (DateTime.UtcNow < _cachedAllDomainsExpires && _cachedAllDomains.Count > 0)
                {
                    return _cachedAllDomains;
                }

                var all = UContext?.Domains?.GetAll(true)?.OrderByDescending(x => x.Name)?.ToList() ?? [];
                lock (_domainsLock)
                {
                    _cachedAllDomains = all;
                    _cachedAllDomainsExpires = DateTime.UtcNow.AddSeconds(CachedAllDomainsSeconds);
                }
                return _cachedAllDomains;
            }

            var idItem = GetItem().Current?.Id;
            var domain = UContext?.Domains?.GetAssigned(idItem ?? 0, true)?.ToList();
            return domain ?? [];
        }

        public void SetCurrentCulture(CultureInfo cul) => _currentCulture = cul;

        public string GetUrlWithCulture(IPublishedContent content, string? culture = null, UrlMode mode = UrlMode.Default)
        {
            var url = content.Url(culture ?? CurrentCulture.Name, mode);

            var domains = GetDomains(true);
            var domain = domains.FirstOrDefault(x =>
                x.Culture?.Equals(culture, StringComparison.OrdinalIgnoreCase) ?? false);

            if (domain != null)
            {
                var domainUrl = !(domain.Name.StartsWith('/')
                    || domain.Name.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || domain.Name.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    ? "https://" + domain.Name
                    : domain.Name;

                var uri = new Uri(domainUrl, UriKind.RelativeOrAbsolute);
                var segment = uri.IsAbsoluteUri ? uri.AbsolutePath.Trim('/') : domainUrl.Trim('/');

                if (!string.IsNullOrWhiteSpace(segment))
                {
                    // Absolute URL (e.g. sitemap uses UrlMode.Absolute): insert the culture
                    // segment right after the host when it is missing so the default culture
                    // also gets its prefix, e.g.
                    // https://utpro.local/gioi-thieu/ -> https://utpro.local/vi/gioi-thieu/
                    if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
                        {
                            var path = absoluteUrl.AbsolutePath.Trim('/');
                            if (!path.Equals(segment, StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith(segment + "/", StringComparison.OrdinalIgnoreCase))
                            {
                                var builder = new UriBuilder(absoluteUrl)
                                {
                                    Path = "/" + segment + absoluteUrl.AbsolutePath
                                };
                                return builder.Uri.ToString();
                            }
                        }
                        return url;
                    }

                    // Relative URL
                    if (!url.StartsWith($"/{segment}", StringComparison.OrdinalIgnoreCase))
                    {
                        if (url == "/")
                            return $"/{segment}";
                        return $"/{segment}{url}";
                    }
                }
            }

            return url;
        }
    }
}
