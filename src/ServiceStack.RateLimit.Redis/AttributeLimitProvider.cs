// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this 
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.RateLimit.Redis
{
    using Configuration;
    using Interfaces;
    using Models;
    using Web;

    public class AttributeLimitProvider : ILimitProvider
    {
        public Limits GetLimits(IRequest request)
        {
            if (request.Items.GetValueOrDefault(RateLimitAttribute.RequestItemName) is Limits limits)
            {
                return limits;
            }

            return new Limits
            {
                Request = LimitProviderConstants.DefaultLimits,
                User = null
            };
        }
    }
}