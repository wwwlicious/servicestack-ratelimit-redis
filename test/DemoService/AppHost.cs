// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace DemoService
{
    using System.Collections.Generic;
    using Funq;
    using ServiceStack;
    using ServiceStack.Auth;
    using ServiceStack.Caching;
    using ServiceStack.Logging;
    using ServiceStack.RateLimit.Redis;
    using ServiceStack.Redis;

    public class AppHost : AppSelfHostBase
    {
        private readonly string serviceUrl;

        public AppHost(string serviceUrl) : base("DemoService", typeof(DemoService).Assembly)
        {
            this.serviceUrl = serviceUrl;
        }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                WebHostUrl = serviceUrl,
                ApiVersion = "2.0"
            });

            LogManager.LogFactory = new ConsoleLogFactory();

            SetupDependencies();
            SetupPlugins();
        }

        private void SetupPlugins()
        {
            /*Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                new IAuthProvider[]
                {
                    new BasicAuthProvider()
                }));*/

            /*LimitKeyGenerator.Delimiter = ":";
            LimitKeyGenerator.Prefix = null;*/
            Plugins.Add(new RateLimitFeature(Container.Resolve<IRedisClientsManager>()));
        }

        private void SetupDependencies()
        {
            // Ubuntu VM running redis
            var redisConnection = AppSettings.GetString("RedisConnectionString");
            Container.Register<IRedisClientsManager>(new BasicRedisClientManager(redisConnection));

            // Setup basic auth
            Container.Register<ICacheClient>(new MemoryCacheClient());
            var userRep = new InMemoryAuthRepository();
            Container.Register<IUserAuthRepository>(userRep);

            RegisterUsers(userRep);
        }

        private static void RegisterUsers(InMemoryAuthRepository userRep)
        {
            // Create a series of fake users
            var usernames = new[] { "Cheetara", "Panthro", "Tygra" };

            foreach (var username in usernames)
            {
                // Create fake users
                if (userRep.GetUserAuthByUserName(username) == null)
                {
                    userRep.CreateUserAuth(new UserAuth
                    {
                        UserName = username,
                        FirstName = $"{username}_test",
                        LastName = "ThunderCat",
                        Roles = new List<string> { "test" }
                    }, "password");
                }
            }
        }
    }
}