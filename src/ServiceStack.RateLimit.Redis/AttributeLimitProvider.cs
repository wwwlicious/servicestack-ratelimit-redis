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
        private readonly IAppSettings appSettings;

        public AttributeLimitProvider(IAppSettings appSettings)
        {
            appSettings.ThrowIfNull(nameof(appSettings));

            this.appSettings = appSettings;
        }

        public Limits GetLimits(IRequest request)
        {
            var limits = request.Items.GetValueOrDefault(LimitRateAttribute.RequestItemName) as Limits;
            if (limits != null)
            {
                return limits;
            }

            return new Limits
            {
                Request = LimitProviderConstants.DefaultLimits,
                User = null
            };
        }

        public string GetRateLimitScriptId()
        {
            return appSettings.GetString(LimitProviderConstants.ScriptKey);
        }
    }
}