// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Headers
{
    public class RateLimitStatus
    {
        public int Seconds { get; }
        public int Limit { get; }
        public int Remaining { get; }

        public RateLimitStatus(int limit, int remaining, int seconds)
        {
            Seconds = seconds;
            Limit = limit;
            Remaining = remaining;
        }
    }
}