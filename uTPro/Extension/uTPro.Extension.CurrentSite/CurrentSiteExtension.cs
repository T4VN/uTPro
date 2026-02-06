using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.UmbracoContext;

namespace uTPro.Extension.CurrentSite
{
    class DICurrentSiteExtension : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ICurrentSiteExtension, CurrentSiteExtension>();
    }

    public interface ICurrentSiteExtension
    {
        IConfiguration Configuration { get; }
        IWebHostEnvironment WebHostEnvironment { get; }
        string DefaultCulture { get; }
        CultureInfo CurrentCulture { get; }
        string CurrentPage { get; }
        IEnumerable<PublishedCultureInfo> GetCultures();
        IUmbracoContext UContext { get; }
        public string GetDictionaryValue(string key, string valueDefault = "", bool showKey = false);
        void SetCurrentCulture(CultureInfo cul);
        ICurrentItemExtension GetItem();
        Task<IEnumerable<Domain>> GetDomains(bool isGetAll);
        string GetUrlWithCulture(IPublishedContent content, string? culture = null, UrlMode mode = UrlMode.Default);
    }

    internal class CurrentSiteExtension : ICurrentSiteExtension
    {
        readonly IServiceScopeFactory _scopeFactory;
        readonly ILogger<CurrentSiteExtension> _logger;
        readonly ICultureDictionary _cultureDictionary;
        readonly IUmbracoContextFactory _umbracoContextFactory;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public CurrentSiteExtension(
            ILogger<CurrentSiteExtension> logger,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IUmbracoContextFactory umbracoContextFactory,
            ICultureDictionary cultureDictionary)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _umbracoContextFactory = umbracoContextFactory;
            _cultureDictionary = cultureDictionary;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public string DefaultCulture
        {
            get
            {
                return UContext.Domains?.DefaultCulture ?? Thread.CurrentThread.CurrentCulture.Name;
            }
        }

        CultureInfo _currentCulture;
        public CultureInfo CurrentCulture
        {
            get
            {
                this._currentCulture ??= Thread.CurrentThread.CurrentCulture;
                return this._currentCulture;
            }
        }

        public string CurrentPage
        {
            get
            {
                return UContext.OriginalRequestUrl.ToString();
            }
        }

        readonly IWebHostEnvironment _webHostEnvironment;
        public IWebHostEnvironment WebHostEnvironment
        {
            get
            {
                return this._webHostEnvironment;
            }
        }

        readonly IConfiguration _configuration;
        public IConfiguration Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        public ICurrentItemExtension GetItem()
        {
            // Resolve ICurrentItemExtension from the DI container so the container can manage its lifetime
            using var scope = _scopeFactory.CreateScope();
            var item = scope.ServiceProvider.GetRequiredService<ICurrentItemExtension>();
            if (item != null)
                return item;

            // Fallback: create a new instance if DI cannot provide one
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CurrentItemExtension>>() ?? NullLogger<CurrentItemExtension>.Instance;
            return new CurrentItemExtension(logger, this);
        }

        IUmbracoContext _umbracoContext;
        public IUmbracoContext UContext
        {
            get
            {
                this._umbracoContext ??= _umbracoContextFactory.EnsureUmbracoContext().UmbracoContext;

                return this._umbracoContext ?? throw new Exception("UmbracoContext is null");
            }
        }

        public IEnumerable<PublishedCultureInfo> GetCultures()
        {
            var culs = this.GetItem().PageHome?.Cultures;
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
            key = this.GetItem().Root.Name + "." + key;
            var dictionaryValue = _cultureDictionary[key];
            if (string.IsNullOrWhiteSpace(dictionaryValue))
            {
                dictionaryValue = showKey ? key : valueDefault;
            }
            return dictionaryValue;
        }

        // Simple in-memory cache for all domains to reduce repeated UContext access
        private static readonly Lock _domainsLock = new();
        private static IEnumerable<Domain> _cachedAllDomains = [];
        private static DateTime _cachedAllDomainsExpires = DateTime.MinValue;
        private const int CachedAllDomainsSeconds = 60;

        public Task<IEnumerable<Domain>> GetDomains(bool isGetAll)
        {
            if (isGetAll)
            {
                // return cached list when available
                if (DateTime.UtcNow < _cachedAllDomainsExpires && _cachedAllDomains != null && _cachedAllDomains.Any())
                {
                    return Task.FromResult(_cachedAllDomains);
                }

                var all = UContext?.Domains?.GetAll(true) ?? [];
                lock (_domainsLock)
                {
                    _cachedAllDomains = all;
                    _cachedAllDomainsExpires = DateTime.UtcNow.AddSeconds(CachedAllDomainsSeconds);
                }
                return Task.FromResult(_cachedAllDomains);
            }

            var idItem = this.GetItem().Current?.Id;
            var domain = UContext?.Domains?.GetAssigned(idItem ?? 0, true);
            return Task.FromResult(domain ?? []);
        }

        public void SetCurrentCulture(CultureInfo cul) => this._currentCulture = cul;

        public string GetUrlWithCulture(IPublishedContent content, string? culture = null, UrlMode mode = UrlMode.Default)
        {
            var url = content.Url(culture ?? this.CurrentCulture.Name, mode);

            var domain = this.GetDomains(true).GetAwaiter().GetResult().FirstOrDefault(x => x.Culture?.Equals(culture, StringComparison.OrdinalIgnoreCase) ?? false);

            if (domain != null)
            {
                var domainUrl = !(domain.Name.StartsWith('/')
                    || domain.Name.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || domain.Name.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    ? "https://" + domain.Name 
                    : domain.Name;

                var uri = new Uri(domainUrl, UriKind.RelativeOrAbsolute);
                var segment = uri.AbsolutePath.Trim('/');

                if (!string.IsNullOrWhiteSpace(segment))
                {
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
