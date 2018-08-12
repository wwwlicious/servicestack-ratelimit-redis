// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    using System.Linq;
    using Configuration;
    using Interfaces;
    using Logging;
    using Models;
    using Utilities;
    using Web;

    public class AppSettingsLimitProvider : ILimitProvider
    {
        private readonly ILimitKeyGenerator keyGenerator;
        private readonly IAppSettings appSettings;
        private readonly ILog log = LogManager.GetLogger(typeof(AppSettingsLimitProvider));

        public AppSettingsLimitProvider(ILimitKeyGenerator keyGenerator, IAppSettings appSettings)
        {
            keyGenerator.ThrowIfNull(nameof(keyGenerator));
            appSettings.ThrowIfNull(nameof(appSettings));

            this.keyGenerator = keyGenerator;
            this.appSettings = appSettings;
        }

        public Limits GetLimits(IRequest request)
        {
            var requestLimits = GetRequestLimits(request);
            var userLimits = GetUserLimits(request);

            return new Limits
            {
                // Return default if none found
                Request = requestLimits.HasValue ? requestLimits.Value : LimitProviderConstants.DefaultLimits,
                User = userLimits.HasValue ? userLimits.Value : null
            };
        }

        public string GetRateLimitScriptId()
        {
            return appSettings.GetString(LimitProviderConstants.ScriptKey);
        }

        protected virtual Maybe<LimitGroup> GetConfigLimit(params string[] keys)
        {
            // Return the first value that is found as keys are in order of precedence
            foreach (var key in keys)
            {
                var limit = appSettings.Get<LimitGroup>(key);
                if (limit != null)
                {
                    return new Maybe<LimitGroup>(limit);
                }
            }

            if (log.IsDebugEnabled)
            {
                log.Debug($"No matching config values found for {keys.ToCsv()}");
            }

            return new Maybe<LimitGroup>();
        }

        private Maybe<LimitGroup> GetRequestLimits(IRequest request)
        {
            var requestKeys = keyGenerator.GetConfigKeysForRequest(request);
            var requestLimits = GetConfigLimit(requestKeys.ToArray());
            return requestLimits;
        }

        private Maybe<LimitGroup> GetUserLimits(IRequest request)
        {
            var userKey = keyGenerator.GetConfigKeysForUser(request);
            var userLimit = GetConfigLimit(userKey.ToArray());
            return userLimit;
        }
    }
}