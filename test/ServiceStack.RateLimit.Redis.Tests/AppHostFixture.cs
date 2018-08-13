using System;
using ServiceStack.Testing;

namespace ServiceStack.RateLimit.Redis.Tests
{
    public class AppHostFixture : IDisposable
    {
        private ServiceStackHost appHost;

        public AppHostFixture()
        {
            appHost = new BasicAppHost().Init();
        }

        public ServiceStackHost AppHost => appHost;

        public void Dispose()
        {
            appHost.Dispose();
        }
    }
}