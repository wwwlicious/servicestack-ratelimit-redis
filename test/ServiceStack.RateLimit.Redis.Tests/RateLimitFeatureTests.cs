// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using FakeItEasy;
    using FluentAssertions;
    using Interfaces;
    using Models;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoFakeItEasy;
    using Ploeh.AutoFixture.Xunit2;
    using ServiceStack;
    using ServiceStack.Redis;
    using Testing;
    using Web;
    using Xunit;

    public class RateLimitFeatureTests
    {
        private readonly RateLimitFeature feature;
        private readonly ILimitKeyGenerator keyGenerator;
        private readonly ILimitProvider limitProvider;
        private readonly IRedisClientsManager redisManager;
        private readonly Limits limit;

        public RateLimitFeatureTests()
        {
            redisManager = A.Fake<IRedisClientsManager>();
            limitProvider = A.Fake<ILimitProvider>();
            keyGenerator = A.Fake<ILimitKeyGenerator>();
            feature = new RateLimitFeature(redisManager, limitProvider, keyGenerator);

            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            limit = fixture.Create<Limits>();
            A.CallTo(() => limitProvider.GetLimits(A<IRequest>.Ignored)).Returns(limit);
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfRedisManagerNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RateLimitFeature(null, A.Fake<ILimitProvider>(), A.Fake<ILimitKeyGenerator>()));
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfLimitProviderNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RateLimitFeature(A.Fake<IRedisClientsManager>(), null, A.Fake<ILimitKeyGenerator>()));
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfKeyGeneratorNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RateLimitFeature(A.Fake<IRedisClientsManager>(), A.Fake<ILimitProvider>(), null));
        }

        [Fact]
        public void Register_AddsPreRequestFilter()
        {
            var appHost = A.Fake<IAppHost>();
            appHost.PreRequestFilters.Count.Should().Be(0);

            feature.Register(appHost);

            appHost.PreRequestFilters.Count.Should().Be(1);
        }

        [Fact]
        public void ProcessRequest_CallsGetLimits()
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).Returns(null);
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_HandlesNullLimit()
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).Returns(null);
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            // No assert here - not throwing is enough (but a bit rubbish)
        }

        [Fact]
        public void ProcessRequest_GetsConsumerId()
        {
            var mockHttpRequest = new MockHttpRequest();

            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => keyGenerator.GetConsumerId(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_GetRequestId()
        {
            var mockHttpRequest = new MockHttpRequest();

            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => keyGenerator.GetRequestId(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_GetsRateLimitScriptFromConfig()
        {
            var mockHttpRequest = new MockHttpRequest();

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

            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => client.LoadLuaScript(A<string>.Ignored)).MustHaveHappened();
        }

        [Theory, InlineAutoData]
        public void ProcessRequest_CallsLuaScript(string sha1)
        {
            var mockHttpRequest = new MockHttpRequest();
            
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => redisManager.GetClient()).Returns(client);

            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse());

            A.CallTo(() => limitProvider.GetRateLimitScriptId()).Returns(sha1);
        }
    }
}
