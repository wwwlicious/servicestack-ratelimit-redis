// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests
{
    using System.Linq;
    using Auth;
    using FakeItEasy;
    using FluentAssertions;
    using Ploeh.AutoFixture.Xunit2;
    using ServiceStack;
    using Testing;
    using Web;
    using Xunit;

    [Collection("RateLimitTests")]
    public class LimitKeyGeneratorTests
    {
        public LimitKeyGeneratorTests()
        {
            if (ServiceStackHost.Instance == null)
            {
                new BasicAppHost().Init();
            }
        }

        private static LimitKeyGenerator GetGenerator()
        {
            var keyGenerator = new LimitKeyGenerator();
            return keyGenerator;
        }

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
            var request = new MockHttpRequest();
            var keyGenerator = GetGenerator();

            Assert.Throws<AuthenticationException>(() => keyGenerator.GetConsumerId(request));
        }

        [Theory, AutoData]
        public void GetConsumerId_ReturnsUserId_IfAuthenticated(string userAuthId)
        {
            MockHttpRequest request = new MockHttpRequest();
            var authSession = SetupAuthenticatedSession(userAuthId, request);

            var keyGenerator = GetGenerator();
            var consumerId = keyGenerator.GetConsumerId(request);

            consumerId.Should().Be(authSession.UserAuthId);
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
        [InlineData("lmt:opname:userId", 0)]
        [InlineData("lmt:opname", 1)]
        [InlineData("lmt:default", 2)]
        public void GetConfigKeysForRequest_ReturnsResultsInOrder(string key, int index)
        {
            const string operationName = "opname";
            const string userAuthId = "userId";

            var request = new MockHttpRequest(operationName, "GET", "text/json", string.Empty, null, null, null);
            SetupAuthenticatedSession(userAuthId, request);

            var keyGenerator = GetGenerator();
            var keys = keyGenerator.GetConfigKeysForRequest(request);

            keys.ToList()[index].Should().Be(key);
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
        [InlineData("lmt:usr:userId", 0)]
        [InlineData("lmt:usr:default", 1)]
        public void GetConfigKeysForUser_ReturnsCorrectNumberOfKeys(string key, int index)
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
            request.Items[SessionFeature.RequestItemsSessionKey] = authSession;
            return authSession;
        }
    }
}
