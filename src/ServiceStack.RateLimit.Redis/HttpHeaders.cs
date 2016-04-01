﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    public static class HttpHeaders
    {
        public const string RateLimitFormat = "X-RateLimit-Limit-{0}";
        public const string RateCurrentFormat = "X-RateLimit-Count-{0}";
    }
}