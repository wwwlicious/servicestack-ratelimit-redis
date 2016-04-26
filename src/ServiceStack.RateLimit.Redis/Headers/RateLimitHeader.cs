// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Headers
{
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    public class RateLimitHeader
    {
        public IEnumerable<RateLimitStatus> Limits { get; private set; }
        public string HeaderName { get; private set; }

        public static IEnumerable<RateLimitHeader> Create(IEnumerable<RateLimitTimeResult> results)
        {
            if (results == null)
            {
                return new RateLimitHeader[0];
            }

            return from result in results
                group result by result.User
                into grp
                select
                    new RateLimitHeader
                    {
                        Limits = grp.Select(r => new RateLimitStatus(r.Limit, r.Remaining, r.Seconds)),
                        HeaderName = grp.Key ? HttpHeaders.RateLimitUser : HttpHeaders.RateLimitRequest
                    };
        }
    }
}