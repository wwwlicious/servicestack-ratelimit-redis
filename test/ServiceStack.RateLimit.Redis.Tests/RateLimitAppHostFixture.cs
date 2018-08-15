using System;
using ServiceStack.Testing;

namespace ServiceStack.RateLimit.Redis.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using FluentAssertions;
    using Funq;
    using RedisInside;
    using ServiceStack.Auth;
    using ServiceStack.Redis;
    using ServiceStack.Web;
    using Xunit;

    public class RateLimitAppHostFixture : IDisposable
    {
        internal readonly RateLimitAppHost Apphost;
        internal readonly string BaseUrl;
        
        public RateLimitAppHostFixture()
        {
            Apphost = new RateLimitAppHost();
            Apphost.Init();
            BaseUrl = "http://localhost:1337/";
            Apphost.Start(BaseUrl);
            Apphost.StartUpErrors.Should().BeNullOrEmpty();
        }

        public IServiceClient CreateClient()
        {
            return new JsonServiceClient(BaseUrl);
        }
        
        public IServiceClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.SetCredentials(Apphost.ValidUser, Apphost.ValidPassword);
            return client;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Apphost?.Dispose();
        }
    }

    public class RateLimitAppHost : AppSelfHostBase
    {
        internal IUserAuth TestUser;
        
        public RateLimitAppHost() : base("RateLimitTestAppHost", typeof(RateLimitedService).Assembly)
        {
            AppSettings = new SimpleAppSettings();
            // global limit default
            AppSettings.Set("ss/lmt/default","{Limits:[{Limit:10,Seconds:60},{Limit:20,Seconds:3600},{Limit:30,Seconds:86400}]}");
            // global user limit default
            AppSettings.Set("ss/lmt/usr/default","{Limits:[{Limit:30,Seconds:60},{Limit:100,Seconds:3600},{Limit:250,Seconds:86400}]}");
            // limit for userId: 1 for all requests
            AppSettings.Set("ss/lmt/usr/1","{Limits:[{Limit:7,Seconds:60},{Limit:15,Seconds:3600},{Limit:40,Seconds:86400}]}");
            // limit for configratelimitrequest for all users
            AppSettings.Set("ss/lmt/configratelimitrequest","{Limits:[{Limit:8,Seconds:60},{Limit:13,Seconds:3600},{Limit:21,Seconds:86400}]}");
            // limit for userId: 1 for configratelimitrequest
            AppSettings.Set("ss/lmt/configratelimitrequest/1","{Limits:[{Limit:5,Seconds:60},{Limit:10,Seconds:3600},{Limit:30,Seconds:86400}]}");
        }

        public string ValidUser { get; set; } = "test";
        public string ValidPassword { get; set; } = "user";

        public override void Configure(Container container)
        {
            // create a valid user for testing
            var authRepository = new InMemoryAuthRepository();
            var user = authRepository.CreateUserAuth();
            user.UserName = ValidUser;
            user.Roles = new List<string> { "test" };
            TestUser = authRepository.CreateUserAuth(user, ValidPassword);
            container.Register<IAuthRepository>(authRepository);

            var instance = new Redis();
            container.Register(instance);
            Container.Register<IRedisClientsManager>(new BasicRedisClientManager(instance.Endpoint.ToString()));

            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] { new BasicAuthProvider(AppSettings) }, "/home"));
            Plugins.Add(new RateLimitFeature(Container.Resolve<IRedisClientsManager>()));
        }
    }
    
    [CollectionDefinition("RateLimitFeature")]
    public class RateLimitFeatureCollection : ICollectionFixture<RateLimitAppHostFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
        
        // To use, add [Collection("RateLimitFeature")] to the test class and
        // add a constructor with the argument (RateLimitAppHostFixture fixture)
    }

    public class RateLimitedService : Service
    {
        public object Any(AttributeRateLimitRequest request) => request;
        public object Any(ConfigRateLimitRequest request) => request;
    }
    
    [Authenticate]
    [RateLimit(2,RatePeriod.PerMinute)]
    public class AttributeRateLimitRequest : IGet, IReturn<AttributeRateLimitRequest>, IRequiresRequest
    {
        public IRequest Request { get; set; }
    }
    
    [Authenticate]
    public class ConfigRateLimitRequest : IReturn<ConfigRateLimitRequest>, IRequiresRequest
    {
        public IRequest Request { get; set; }
    }
}