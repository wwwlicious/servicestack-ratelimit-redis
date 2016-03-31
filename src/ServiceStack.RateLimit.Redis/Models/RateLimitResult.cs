// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Models
{
    using System.Diagnostics;

    /// <summary>
    /// Overall return object from Lua script - containing whether access should be granted and breakdown of limits
    /// </summary>
    [DebuggerDisplay("{Access}")]
    public class RateLimitResult
    {
        public bool? Access { get; set; }
        public RateLimitTimeResult[] Results { get; set; } = new RateLimitTimeResult[0];
    }

    /// <summary>
    /// Detail of individual limit from Lua script
    /// </summary>
    [DebuggerDisplay("{Duration} - {Current}/{Limit}")]
    public class RateLimitTimeResult
    {
        public int Limit { get; set; }
        public int Duration { get; set; } // TODO Get a nicer representation of this
        public int Current { get; set; }
        public bool User { get; set; }
    }
}