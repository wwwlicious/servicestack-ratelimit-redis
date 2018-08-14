// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using System.Linq;
    using Auth;
    using FakeItEasy;
    using FluentAssertions;
    using AutoFixture.Xunit2;
    using ServiceStack;
    using Testing;
    using Web;
    using Xunit;

    [Collection("RateLimitFeature")]
    public class LimitKeyGeneratorTests
    {
        
        public LimitKeyGeneratorTests(RateLimitAppHostFixture fixture)
        {
            // The GetConsumerId method requires an AuthUserSession.
            //AuthenticateService.Init(() => new AuthUserSession(), new BasicAuthProvider(fixture.AppHost.AppSettings));
        }

        private static LimitKeyGenerator GetGenerator() => new LimitKeyGenerator();

        [Theory, AutoData]
        public void GetRequestId_ReturnsOperationName(string operationName)
        {
            var request = A.Fake<IRequest>();
            A.CallTo(() => request.OperationName).Returns(operationName);

            var keyGenerator = GetGenerator();

            var requestId = keyGenerator.GetRequestId(request);

            requestId.Should().BeEquivalentTo(request.OperationName);
        }

        [Fact]
        public void GetConsumerId_ThrowsAuthenticationException_IfNotAuthenticated()
        {
            var keyGenerator = GetGenerator();

            Action action = () => keyGenerator.GetConsumerId(new MockHttpRequest());

            action.Should().Throw<AuthenticationException>();
        }

        [Theory, AutoData]
        public void GetConsumerId_ReturnsUserId_IfAuthenticated(string userAuthId)
        {
            MockHttpRequest request = new MockHttpRequest();
            var authSession = SetupAuthenticatedSession(userAuthId, request);

            var keyGenerator = GetGenerator();
            var consumerId = keyGenerator.GetConsumerId(request);

            consumerId.Should().Be(authSession.UserAuthId.ToLower());
        }

        [Fact]
        public void GetConfigKeysForRequest_ReturnsCorrectNumberOfKeys()
        {
            MockHttpRequest request = new MockHttpRequest();
            SetupAuthenticatedSession("123", request);

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForRequest(request);

            keys.Count().Should().Be(3);
        }

        [Theory]
        [InlineData("ss/lmt/opname/userId", 0)]
        [InlineData("ss/lmt/opname", 1)]
        [InlineData("ss/lmt/default", 2)]
        public void GetConfigKeysForRequest_ReturnsResultsInOrder(string key, int index)
        {
            const string operationName = "opname";
            const string userAuthId = "userId";

            var request = new MockHttpRequest(operationName, "GET", "text/json", string.Empty, null, null, null);
            SetupAuthenticatedSession(userAuthId, request);

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForRequest(request);

            keys.ToList()[index].Should().Be(key.ToLower());
        }

        [Theory]
        [InlineData("lmt:opname:userId", 0)]
        [InlineData("lmt:opname", 1)]
        [InlineData("lmt:default", 2)]
        public void GetConfigKeysForRequest_ReturnsResultsInOrder_ObeyDelimiterAndPrefix(string key, int index)
        {
            const string operationName = "opname";
            const string userAuthId = "userId";
            LimitKeyGenerator.Delimiter = ":";
            LimitKeyGenerator.Prefix = null;

            var request = new MockHttpRequest(operationName, "GET", "text/json", string.Empty, null, null, null);
            SetupAuthenticatedSession(userAuthId, request);

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForRequest(request);

            keys.ToList()[index].Should().Be(key.ToLower());

            // Now set the values back as they're static (avoid breaking tests)
            LimitKeyGenerator.Delimiter = "/";
            LimitKeyGenerator.Prefix = "ss";
        }

        [Fact]
        public void GetConfigKeysForUser_ReturnsCorrectNumberOfKeys()
        {
            MockHttpRequest request = new MockHttpRequest();
            SetupAuthenticatedSession("123", request);

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForUser(request);

            keys.Count().Should().Be(2);
        }

        [Theory]
        [InlineData("test|lmt|usr|userid", 0)]
        [InlineData("test|lmt|usr|default", 1)]
        public void GetConfigKeysForUser_ReturnsResultsInOrder_ObeyDelimiterAndPrefix(string key, int index)
        {
            const string userAuthId = "userId";
            MockHttpRequest request = new MockHttpRequest();
            SetupAuthenticatedSession(userAuthId, request);

            LimitKeyGenerator.Delimiter = "|";
            LimitKeyGenerator.Prefix = "test";

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForUser(request);

            keys.ToList()[index].Should().Be(key);

            // Now set the values back as they're static (avoid breaking tests)
            LimitKeyGenerator.Delimiter = "/";
            LimitKeyGenerator.Prefix = "ss";
        }

        [Theory]
        [InlineData("ss/lmt/usr/userid", 0)]
        [InlineData("ss/lmt/usr/default", 1)]
        public void GetConfigKeysForUser_ReturnsResultsInOrder(string key, int index)
        {
            const string userAuthId = "userId";
            MockHttpRequest request = new MockHttpRequest();
            SetupAuthenticatedSession(userAuthId, request);

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForUser(request);

            keys.ToList()[index].Should().Be(key);
        }

        private static IAuthSession SetupAuthenticatedSession(string userAuthId, IRequest request)
        {
            var authSession = A.Fake<IAuthSession>();
            A.CallTo(() => authSession.IsAuthenticated).Returns(true);
            A.CallTo(() => authSession.UserAuthId).Returns(userAuthId);

            // From http://stackoverflow.com/questions/34064277/passing-session-in-unit-test
            request.Items[Keywords.Session] = authSession;
            return authSession;
        }
    }

    [Collection("LimitKeyGeneratorTests")]
    public class LimitKeyGeneratorHostlessTests : IDisposable
    {
        private readonly AppDomain noAuthDomain;
        public LimitKeyGeneratorHostlessTests()
        {
            noAuthDomain = AppDomain.CreateDomain("NoAuthDomain", AppDomain.CurrentDomain.Evidence,
                AppDomain.CurrentDomain.SetupInformation);
        }

        [Fact(Skip = "maybe move authprovider check to apphost startup and call explicitly in fake apphost to test")]
        public void GetConsumerId_ThrowsInvalidOperationException_IfNoAuthProviders()
        {
            // NOTE Running this in separate apphost as internally checks a public static prop which may have been set in other tests
            noAuthDomain.DoCallBack(() =>
            {
                var keyGenerator = new LimitKeyGenerator();

                Action action = () => keyGenerator.GetConsumerId(new MockHttpRequest());

                action.Should().Throw<InvalidOperationException>();
            });
        }

        public void Dispose()
        {
            if (noAuthDomain != null)
            {
                AppDomain.Unload(noAuthDomain);
            }
        }
    }
}
