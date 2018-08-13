// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using FakeItEasy;
    using FluentAssertions;
    using Interfaces;
    using AutoFixture;
    using AutoFixture.AutoFakeItEasy;
    using AutoFixture.Xunit2;
    using Redis.Models;
    using ServiceStack;
    using ServiceStack.Redis;
    using Testing;
    using Web;
    using Xunit;

    public class RateLimitFeatureTests : IClassFixture<AppHostFixture>
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
            Action action = () => new RateLimitFeature(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Register_SetsDefaultLimitKeyGenerator_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            feature.KeyGenerator.Should().BeOfType<LimitKeyGenerator>();
        }

        [Fact]
        public void Register_DoesNotSetDefaultLimitKeyGenerator_IfSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.KeyGenerator = keyGenerator;
            feature.Register(appHost);

            feature.KeyGenerator.Should().Be(keyGenerator);
        }

        [Fact]
        public void Register_SetsDefaultLimitProvider_IfNotSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.Register(appHost);

            feature.LimitProvider.Should().BeOfType<AppSettingsLimitProvider>();
        }

        [Fact]
        public void Register_DoesNotSetDefaultLimitProviderBase_IfSet()
        {
            var appHost = A.Fake<IAppHost>();
            var feature = GetSut(false);
            feature.LimitProvider = limitProvider;
            feature.Register(appHost);

            feature.LimitProvider.Should().Be(limitProvider);
        }

        [Fact]
        public void Register_AddsGlobalRequestFilter()
        {
            var appHost = A.Fake<IAppHost>();
            appHost.GlobalRequestFilters.Count.Should().Be(0);

            var feature = GetSut();
            feature.Register(appHost);

            appHost.GlobalRequestFilters.Count.Should().Be(1);
        }

        [Fact]
        public void ProcessRequest_CallsGetLimits()
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).Returns(null);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_HandlesNullLimit()
        {
            var mockHttpRequest = new MockHttpRequest();
            A.CallTo(() => limitProvider.GetLimits(mockHttpRequest)).Returns(null);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

            // No assert here - not throwing is enough
        }

        [Fact]
        public void ProcessRequest_GetsConsumerId()
        {
            var mockHttpRequest = new MockHttpRequest();

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

            A.CallTo(() => keyGenerator.GetConsumerId(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_GetRequestId()
        {
            var mockHttpRequest = new MockHttpRequest();

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

            A.CallTo(() => keyGenerator.GetRequestId(mockHttpRequest)).MustHaveHappened();
        }

        [Fact]
        public void ProcessRequest_GetsRateLimitScriptFromConfig()
        {
            var mockHttpRequest = new MockHttpRequest();

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

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
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

            A.CallTo(() => client.LoadLuaScript(A<string>.Ignored)).MustHaveHappened();
        }

        [Theory, InlineAutoData]
        public void ProcessRequest_ExecutesLuaScript(string sha1)
        {
            var mockHttpRequest = new MockHttpRequest();
            
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => redisManager.GetClient()).Returns(client);
            A.CallTo(() => limitProvider.GetRateLimitScriptId()).Returns(sha1);

            var feature = GetSut();
            feature.ProcessRequest(mockHttpRequest, new MockHttpResponse(), null);

            A.CallTo(() => client.ExecLuaSha(sha1, A<string[]>.Ignored, A<string[]>.Ignored)).MustHaveHappened();
        }

        [Theory, InlineAutoData]
        public void ProcessRequest_ExecutesLuaScriptWithLimit(string sha1, RateLimitResult rateLimitResult)
        {
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => redisManager.GetClient()).Returns(client);
            A.CallTo(() => limitProvider.GetRateLimitScriptId()).Returns(sha1);

            A.CallTo(() => client.ExecLuaSha(A<string>.Ignored, A<string[]>.Ignored, A<string[]>.Ignored))
                .Returns(new RedisText { Text = rateLimitResult.ToJson() });

            var feature = GetSut();
            var mockHttpResponse = new MockHttpResponse();
            feature.ProcessRequest(new MockHttpRequest(), mockHttpResponse, null);

            mockHttpResponse.Headers[Redis.Headers.HttpHeaders.RateLimitUser].Should().NotBeNullOrWhiteSpace();
            mockHttpResponse.Headers[Redis.Headers.HttpHeaders.RateLimitRequest].Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ProcessRequest_Returns429_IfLimitBreached()
        {
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => client.ExecLuaSha(A<string>.Ignored, A<string[]>.Ignored, A<string[]>.Ignored))
                .Returns(new RedisText { Text = new RateLimitResult { Access = false }.ToJson() });

            var feature = GetSut();
            var response = new MockHttpResponse();

            feature.ProcessRequest(new MockHttpRequest(), response, null);

            response.StatusCode.Should().Be(429);
        }

        [Fact]
        public void ProcessRequest_ReturnsCustomCode_IfSetAndLimitBreached()
        {
            const int statusCode = 503;
            var client = A.Fake<IRedisClient>();
            A.CallTo(() => client.ExecLuaSha(A<string>.Ignored, A<string[]>.Ignored, A<string[]>.Ignored))
                .Returns(new RedisText { Text = new RateLimitResult { Access = false }.ToJson() });

            var feature = GetSut();
            feature.LimitStatusCode = statusCode;
            var response = new MockHttpResponse();

            feature.ProcessRequest(new MockHttpRequest(), response, null);

            response.StatusCode.Should().Be(statusCode);
        }

        [Fact]
        public void ProcessRequest_CallsRequestIdDelegate_IfProvided()
        {
            bool called = false;
            var feature = GetSut();
            feature.CorrelationIdExtractor = request =>
            {
                called = true;
                return "124";
            };

            feature.ProcessRequest(new MockHttpRequest(), new MockHttpResponse(), null);
            called.Should().BeTrue();
        }
    }
}
