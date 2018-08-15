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

    [Collection("RateLimitFeature")]
    public class RateLimitFeatureTests
    {
        private readonly RateLimitFeature rateLimitFeature;
        private IServiceClient client;
        private IServiceClient authenticatedClient;

        public RateLimitFeatureTests(RateLimitAppHostFixture fixture)
        {
            client = fixture.CreateClient();
            authenticatedClient = fixture.CreateAuthenticatedClient();
            
            rateLimitFeature = fixture.Apphost.GetPlugin<RateLimitFeature>();
            
            //rateLimitFeature.LimitProviders.Should().HaveCount(2);
            rateLimitFeature.KeyGenerator.Should().BeOfType<LimitKeyGenerator>();
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfRedisManagerNull()
        {
            Action action = () => new RateLimitFeature(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ProcessRequest_ExecutesLuaScriptWithLimit()
        {
            var result = authenticatedClient.Send(new ConfigRateLimitRequest());

            result.Request.Headers[Redis.Headers.HttpHeaders.RateLimitUser].Should().NotBeNullOrWhiteSpace();
            result.Request.Headers[Redis.Headers.HttpHeaders.RateLimitRequest].Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ProcessRequest_Returns429_IfLimitBreached()
        {
            var response = new MockHttpResponse();

            rateLimitFeature.ProcessRequest(new MockHttpRequest(), response, null);

            response.StatusCode.Should().Be(429);
        }

        [Fact]
        public void ProcessRequest_ReturnsCustomCode_IfSetAndLimitBreached()
        {
            const int statusCode = 503;
            var defaultCode = rateLimitFeature.LimitStatusCode;
            rateLimitFeature.LimitStatusCode = statusCode;
            
            var response = new MockHttpResponse();

            rateLimitFeature.ProcessRequest(new MockHttpRequest(), response, null);

            response.StatusCode.Should().Be(statusCode);
            
            rateLimitFeature.LimitStatusCode = defaultCode;
        }

        [Fact]
        public void ProcessRequest_CallsRequestIdDelegate_IfProvided()
        {
            bool called = false;
            rateLimitFeature.CorrelationIdExtractor = request =>
            {
                called = true;
                return "124";
            };

            rateLimitFeature.ProcessRequest(new MockHttpRequest(), new MockHttpResponse(), null);
            called.Should().BeTrue();
        }
    }
}
