// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using FakeItEasy;
    using FluentAssertions;
    using Interfaces;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoFakeItEasy;
    using Ploeh.AutoFixture.Xunit2;
    using Redis.Models;
    using ServiceStack;
    using ServiceStack.Redis;
    using Testing;
    using Web;
    using Xunit;

    public class RateLimitFeatureTests
    {
        private readonly ILimitKeyGenerator keyGenerator;
        private readonly ILimitProvider limitProvider;
        private readonly IRedisClientsManager redisManager;
        private readonly Limits limit;

        public RateLimitFeatureTests()
        {
            redisManager = A.Fake<IRedisClientsManager>();
            limitProvider = A.Fake<ILimitProvider>();
            keyGenerator = A.Fake<ILimitKeyGenerator>();

            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            limit = fixture.Create<Limits>();
            A.CallTo(() => limitProvider.GetLimits(A<IRequest>.Ignored)).Returns(limit);
        }

        private RateLimitFeature GetSut(bool setupDefaults = true)
        {
            var feature = new RateLimitFeature(redisManager);

            if (setupDefaults)
            {
                feature.LimitProvider = limitProvider;
                feature.KeyGenerator = keyGenerator;
            }
            return feature;
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfRedisManagerNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RateLimitFeature(null));
        }

        [Fact]
        public void Register_RegistersDefaultLimitKeyGenerator_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            A.CallTo(() => appHost.RegisterAs<LimitKeyGenerator, ILimitKeyGenerator>()).MustHaveHappened();
        }

        [Fact]
        public void Register_ResolvesDefaultLimitKeyGenerator_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            A.CallTo(() => appHost.TryResolve<ILimitKeyGenerator>()).MustHaveHappened();
        }

        [Fact]
        public void Register_SetsResolvedDefaultLimitKeyGenerator_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            A.CallTo(() => appHost.TryResolve<ILimitKeyGenerator>()).Returns(keyGenerator);

            var feature = GetSut(false);
            feature.Register(appHost);

            feature.KeyGenerator.Should().Be(keyGenerator);
        }

        [Fact]
        public void Register_ThrowsArgumentNull_IfKeyGeneratorNotSetAndNotResolved()
        {
            var appHost = A.Fake<IAppHost>();
            A.CallTo(() => appHost.TryResolve<ILimitKeyGenerator>()).Returns(null);

            var feature = GetSut(false);

            Assert.Throws<ArgumentNullException>("KeyGenerator", () => feature.Register(appHost));
        }

        [Fact]
        public void Register_DoesNotRegisterDefaultLimitKeyGenerator_IfSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.KeyGenerator = keyGenerator;
            feature.Register(appHost);

            A.CallTo(() => appHost.RegisterAs<LimitKeyGenerator, ILimitKeyGenerator>()).MustNotHaveHappened();
        }

        [Fact]
        public void Register_Resolves_LimitKeyGenerator_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            A.CallTo(() => appHost.RegisterAs<LimitKeyGenerator, ILimitKeyGenerator>()).MustHaveHappened();
        }

        [Fact]
        public void Register_ResolvesDefaultLimitProvider_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            A.CallTo(() => appHost.TryResolve<ILimitProvider>()).MustHaveHappened();
        }

        [Fact]
        public void Register_SetsResolvedDefaultLimitProvider_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            A.CallTo(() => appHost.TryResolve<ILimitProvider>()).Returns(limitProvider);

            var feature = GetSut(false);
            feature.Register(appHost);

            feature.LimitProvider.Should().Be(limitProvider);
        }

        [Fact]
        public void Register_ThrowsArgumentNull_IfLimitProviderNotSetAndNotResolved()
        {
            var appHost = A.Fake<IAppHost>();
            A.CallTo(() => appHost.TryResolve<ILimitProvider>()).Returns(null);

            var feature = GetSut(false);

            Assert.Throws<ArgumentNullException>("LimitProvider", () => feature.Register(appHost));
        }

        [Fact]
        public void Register_RegistersDefaultLimitProviderBase_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            A.CallTo(() => appHost.RegisterAs<LimitProviderBase, ILimitProvider>()).MustHaveHappened();
        }

        [Fact]
        public void Register_DoesNotRegisterDefaultLimitProviderBase_IfSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.LimitProvider = limitProvider;
            feature.Register(appHost);

            A.CallTo(() => appHost.RegisterAs<LimitProviderBase, ILimitProvider>()).MustNotHaveHappened();
        }

        [Fact]
        public void Register_AddsPreRequestFilter()
        {
            var appHost = A.Fake<IAppHost>();
            appHost.PreRequestFilters.Count.Should().Be(0);

            var feature = GetSut();
            feature.Register(appHost);

            appHost.PreRequestFilters.Count.Should().Be(1);
        }

        [Fact]
        public void ProcessRequest_CallsGetLimits()
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).Returns(null);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_HandlesNullLimit()
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).Returns(null);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            // No assert here - not throwing is enough
        }

        [Fact]
        public void ProcessRequest_GetsConsumerId()
        {
            var mockHttpRequest = new MockHttpRequest();

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => keyGenerator.GetConsumerId(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_GetRequestId()
        {
            var mockHttpRequest = new MockHttpRequest();

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => keyGenerator.GetRequestId(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_GetsRateLimitScriptFromConfig()
        {
            var mockHttpRequest = new MockHttpRequest();

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => limitProvider.GetRateLimitScriptId()).MustHaveHappened();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ProcessRequest_RegistersNewScript_IfNoneInConfig(string sha1)
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetRateLimitScriptId()).Returns(sha1);
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => redisManager.GetClient()).Returns(client);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => client.LoadLuaScript(A<string>.Ignored)).MustHaveHappened();
        }

        [Theory, InlineAutoData]
        public void ProcessRequest_CallsLuaScript(string sha1)
        {
            var mockHttpRequest = new MockHttpRequest();
            
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => redisManager.GetClient()).Returns(client);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => limitProvider.GetRateLimitScriptId()).Returns(sha1);
        }
    }
}
