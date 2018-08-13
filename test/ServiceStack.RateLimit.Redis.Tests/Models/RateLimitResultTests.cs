// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests.Models
{
    using FluentAssertions;
    using Redis.Models;
    using Xunit;

    public class RateLimitResultTests
    {
        [Fact]
        public void Results_DefaultedToEmptyArray()
        {
            var results = new RateLimitResult();
            results.Results.Should().BeEmpty();
        }
    }
}
