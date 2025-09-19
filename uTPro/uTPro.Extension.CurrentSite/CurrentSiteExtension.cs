﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Umbraco.Cms.Core.Composing;
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
    }

    internal class CurrentSiteExtension : ICurrentSiteExtension
    {
        readonly IServiceProvider _serviceProvider;
        readonly ILogger<CurrentSiteExtension> _logger;
        readonly ICultureDictionary _cultureDictionary;
        readonly IUmbracoContextFactory _umbracoContextFactory;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public CurrentSiteExtension(
            ILogger<CurrentSiteExtension> logger,
            IServiceProvider serviceProvider,
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
            _serviceProvider = serviceProvider;
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
                if (this._currentCulture == null)
                {
                    this._currentCulture = Thread.CurrentThread.CurrentCulture;
                }
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
            var logger = _serviceProvider.GetService<ILogger<CurrentItemExtension>>();
            using (var current = new CurrentItemExtension(logger, this))
            {
                return current;
            }
        }


        IUmbracoContext _umbracoContext;
        public IUmbracoContext UContext
        {
            get
            {
                if (this._umbracoContext == null)
                {
                    this._umbracoContext = _umbracoContextFactory.EnsureUmbracoContext().UmbracoContext;
                }

                return this._umbracoContext ?? throw new Exception("UmbracoContext is null");
            }
        }


        public IEnumerable<PublishedCultureInfo> GetCultures()
        {
            var culs = this.GetItem().Root.Cultures;
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

        public Task<IEnumerable<Domain>> GetDomains(bool isGetAll)
        {
            return Task.Run<IEnumerable<Domain>>(() =>
            {
                if (isGetAll)
                {
                    return UContext?.Domains?.GetAll(true) ?? new List<Domain>();
                }
                var idItem = this.GetItem().Current?.Id;
                var domain = UContext?.Domains?.GetAssigned(idItem ?? 0, true);
                return domain ?? new List<Domain>();
            });
        }

        public void SetCurrentCulture(CultureInfo cul) => this._currentCulture = cul;
    }
}
