// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests.Models
{
    using FluentAssertions;
    using Redis.Models;
    using Xunit;

    public class RateLimitTimeResultTests : IClassFixture<AppHostFixture>
    {
        [Fact]
        public void Remaining_ReturnsCorrect()
        {
            var result = new RateLimitTimeResult { Current = 10, Limit = 15 };

            result.Remaining.Should().Be(5);
        }

        [Fact]
        public void Remaining_Returns0_IfCurrentMoreThanLimit()
        {
            var result = new RateLimitTimeResult { Current = 11, Limit = 10 };

            result.Remaining.Should().Be(0);
        }
    }
}