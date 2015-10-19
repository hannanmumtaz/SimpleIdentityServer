﻿using System.Linq;
using System.Web.Http;
using System.Web.Http.Filters;
using Microsoft.Owin.Testing;
using Microsoft.Practices.EnterpriseLibrary.Caching;
using Microsoft.Practices.Unity;

using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.Operations;
using SimpleIdentityServer.RateLimitation.Configuration;

namespace SimpleIdentityServer.Api.Tests.Common
{
    public class ConfigureWebApi
    {
        private readonly UnityContainer _container;

        public ConfigureWebApi()
        {
            _container = new UnityContainer();
            _container.RegisterType<ICacheManager, CacheManager>();
            _container.RegisterType<ISecurityHelper, SecurityHelper>();
            _container.RegisterType<ITokenHelper, TokenHelper>();
            _container.RegisterType<IValidatorHelper, ValidatorHelper>();
            _container
                .RegisterType<IGetTokenByResourceOwnerCredentialsGrantType, GetTokenByResourceOwnerCredentialsGrantType>
                ();
        }

        public UnityContainer Container
        {
            get { return _container; }
        }

        public IGetRateLimitationElementOperation GetRateLimitationElementOperation { get; private set; }

        public TestServer CreateServer()
        {
            return TestServer.Create(app =>
            {
                var configuration = new HttpConfiguration();
                RegisterFilterInjector(configuration, _container);
                configuration.DependencyResolver = new UnityResolver(_container);
                WebApiConfig.Register(configuration, app);
            });
        }

        private static void RegisterFilterInjector(HttpConfiguration config, IUnityContainer container)
        {
            //Register the filter injector
            var providers = config.Services.GetFilterProviders().ToList();
            var defaultprovider = providers.Single(i => i is ActionDescriptorFilterProvider);
            config.Services.Remove(typeof(IFilterProvider), defaultprovider);
            config.Services.Add(typeof(IFilterProvider), new UnityFilterProvider(container));
        }
    }
}