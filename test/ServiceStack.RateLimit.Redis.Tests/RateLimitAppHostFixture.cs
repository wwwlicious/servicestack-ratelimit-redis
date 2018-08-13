using System;
using ServiceStack.Testing;

namespace ServiceStack.RateLimit.Redis.Tests
{
    using System.Reflection;
    using FluentAssertions;
    using Funq;
    using RedisInside;
    using ServiceStack.Auth;
    using ServiceStack.Redis;
    using Xunit;

    public class RateLimitAppHostFixture : IDisposable
    {
        internal readonly RateLimitAppHost Apphost;
        internal readonly string BaseUrl;
        
        public RateLimitAppHostFixture()
        {
            AppHost = new RateLimitAppHost();
            Apphost.Init();
            BaseUrl = "http://localhost:1337/";
            Apphost.Start(BaseUrl);
            Apphost.StartUpErrors.Should().BeNullOrEmpty();
        }

        public ServiceStackHost AppHost { get; }
        
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
        
        public RateLimitAppHost() : base("RateLimitTestAppHost", typeof(RateLimitAppHost).Assembly)
        {
        }

        public string ValidUser { get; set; } = "test";
        public string ValidPassword { get; set; } = "user";

        public override void Configure(Container container)
        {
            // create a valid user for testing
            var authRepository = new InMemoryAuthRepository();
            TestUser = authRepository.CreateUserAuth();
            TestUser.UserName = ValidUser;
            authRepository.CreateUserAuth(TestUser, ValidPassword);
            container.Register<IAuthRepository>(authRepository);

            var instance = new Redis();
            container.Register(instance);

            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] { new BasicAuthProvider(AppSettings) }, "/home"));
            Plugins.Add(new RateLimitFeature(new BasicRedisClientManager()));
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
        public object Any(ConfigBasedRateLimitRequest request)
        {
            return request;
        }
    }
    
    [Authenticate]
    public class ConfigBasedRateLimitRequest : IGet, IReturn<ConfigBasedRateLimitRequest>
    {
    }
}