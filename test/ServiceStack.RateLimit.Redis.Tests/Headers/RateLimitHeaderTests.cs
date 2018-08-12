// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests.Headers
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using AutoFixture.Xunit2;
    using Redis.Headers;
    using Redis.Models;
    using Xunit;

    public class RateLimitHeaderTests
    {
        [Fact]
        public void Create_ReturnsEmptyList_IfPassedNull()
        {
            RateLimitHeader.Create(null).Should().BeEmpty();
        }

        [Fact]
        public void Create_ReturnsEmptyList_IfPassedEmpty()
        {
            RateLimitHeader.Create(new RateLimitTimeResult[0]).Should().BeEmpty();
        }

        [Fact]
        public void Create_ReturnsCorrect_Values()
        {
            var results = new List<RateLimitTimeResult>
            {
                new RateLimitTimeResult { Current = 1, Limit = 10, Seconds = 60, User = true },
                new RateLimitTimeResult { Current = 3, Limit = 5, Seconds = 60, User = false }
            };

            var rateLimitHeaders = RateLimitHeader.Create(results);

            rateLimitHeaders.Count().Should().Be(2);
        }

        [Fact]
        public void Create_ReturnsCorrect_HeaderName_User()
        {
            var results = new List<RateLimitTimeResult>
            {
                new RateLimitTimeResult { User = true }
            };

            var rateLimitHeaders = RateLimitHeader.Create(results);

            rateLimitHeaders.First().HeaderName.Should().Be(HttpHeaders.RateLimitUser);
        }

        [Theory]
        [InlineAutoData(false, HttpHeaders.RateLimitRequest)]
        [InlineAutoData(true, HttpHeaders.RateLimitUser)]
        public void Create_ReturnsCorrect_HeaderName_Request(bool isUser, string headerName)
        {
            var results = new List<RateLimitTimeResult>
            {
                new RateLimitTimeResult { User = isUser }
            };

            var rateLimitHeaders = RateLimitHeader.Create(results);

            rateLimitHeaders.First().HeaderName.Should().Be(headerName);
        }
    }
}
