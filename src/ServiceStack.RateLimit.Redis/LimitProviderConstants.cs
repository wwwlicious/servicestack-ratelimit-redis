// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    using System.Collections.Generic;
    using Models;

    public class LimitProviderConstants
    {
        public const string ScriptKey = "script:ratelimit";
        public const int DefaultPerMinute = 10;
        public const int DefaultPerHour = 30;

        public static readonly LimitGroup DefaultLimits=new LimitGroup
        {
            Limits = new List<LimitPerSecond>
            {
                new LimitPerSecond { Seconds = 60, Limit = DefaultPerMinute },
                new LimitPerSecond { Seconds = 3600, Limit = DefaultPerHour }
            }
        };
    }
}