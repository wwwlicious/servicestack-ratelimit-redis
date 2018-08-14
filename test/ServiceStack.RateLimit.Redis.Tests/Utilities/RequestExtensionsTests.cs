// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace ServiceStack.RateLimit.Redis.Tests.Utilities
{
    using FluentAssertions;
    using AutoFixture.Xunit2;
    using Redis.Utilities;
    using Testing;
    using Web;
    using Xunit;

    [Collection("RateLimitFeature")]
    public class RequestExtensionsTests
    {
        [Theory, InlineAutoData]
        public void GetRequestCorrelationId_ReturnsRequestFromHeader(string header)
        {
            var request = new MockHttpRequest();
            request.Headers.Add("x-mac-requestid", header);

            var value = request.GetRequestCorrelationId();
            value.Should().Be(header);
        }

        [Fact]
        public void GetRequestCorrelationId_ReturnsNull_IfNullRequest()
        {
            IRequest request = null;
            request.GetRequestCorrelationId().Should().BeNull();
        }

        [Fact]
        public void GetRequestCorrelationId_ReturnsNull_IfHeaderMissing()
        {
            var request = new MockHttpRequest();
            request.GetRequestCorrelationId().Should().BeNull();
        }

        [Theory, InlineAutoData]
        public void GetRequestCorrelationId_UsesHeaderNameFromFeature(string header)
        {
            string defaultHeaderName = RateLimitFeature.CorrelationIdHeader;

            const string headerName = "sunkilmoon";
            RateLimitFeature.CorrelationIdHeader = headerName;

            var request = new MockHttpRequest();
            request.Headers.Add(headerName, header);

            var value = request.GetRequestCorrelationId();
            value.Should().Be(header);

            RateLimitFeature.CorrelationIdHeader = defaultHeaderName;
        }
    }
}
