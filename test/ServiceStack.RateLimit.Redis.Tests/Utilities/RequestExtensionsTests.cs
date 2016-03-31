// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests.Utilities
{
    using FluentAssertions;
    using Ploeh.AutoFixture.Xunit2;
    using Redis.Utilities;
    using Testing;
    using Web;
    using Xunit;

    public class RequestExtensionsTests
    {
        [Theory, InlineAutoData]
        public void GetRequestId_ReturnsRequestFromHeader(string header)
        {
            var request = new MockHttpRequest();
            request.Headers.Add("x-mac-requestid", header);

            var value = request.GetRequestId();
            value.Should().Be(header);
        }

        [Fact]
        public void GetRequestId_ReturnsEmptyString_IfNullRequest()
        {
            IRequest request = null;
            request.GetRequestId().Should().BeNullOrEmpty();
        }

        [Fact]
        public void GetRequestId_ReturnsEmptyString_IfHeaderMissing()
        {
            var request = new MockHttpRequest();
            request.GetRequestId().Should().BeNullOrEmpty();
        }
    }
}
