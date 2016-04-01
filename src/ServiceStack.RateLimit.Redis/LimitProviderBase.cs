// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Interfaces;
    using Logging;
    using Models;
    using Web;

    public class LimitProviderBase : ILimitProvider
    {
        private readonly ILimitKeyGenerator keyGenerator;
        private const string ScriptKey = "script:ratelimit";

        private const int DefaultPerMinute = 10;
        private const int DefaultPerHour = 30;
        private const int DefaultPerDay = 100;
        private readonly LimitGroup defaultLimits;
        private readonly IAppSettings appSettings;
        private readonly ILog log;

        public LimitProviderBase(ILimitKeyGenerator keyGenerator, IAppSettings appSettings)
        {
            keyGenerator.ThrowIfNull(nameof(keyGenerator));
            appSettings.ThrowIfNull(nameof(appSettings));

            this.keyGenerator = keyGenerator;
            this.appSettings = appSettings;

            log = LogManager.GetLogger(typeof(LimitProviderBase));

            // This is purely to ensure that we always have a default limit
            defaultLimits = new LimitGroup
            {
                Limits = new List<LimitPerSecond>
                {
                    new LimitPerSecond { Seconds = 60, Limit = DefaultPerMinute },
                    new LimitPerSecond { Seconds = 3600, Limit = DefaultPerHour },
                    new LimitPerSecond { Seconds = 86400, Limit = DefaultPerDay }
                }
            };
        }

        public Limits GetLimits(IRequest request)
        {
            var requestLimits = GetRequestLimits(request);
            var userLimits = GetUserLimits(request);

            return new Limits
            {
                // Return default if none found
                Request = requestLimits ?? defaultLimits,
                User = userLimits
            };
        }

        public string GetRateLimitScriptId()
        {
            return appSettings.GetString(ScriptKey);
        }

        protected virtual LimitGroup GetConfigLimit(params string[] keys)
        {
            // Return the first value that is found as keys is in order of precedence
            foreach (var key in keys)
            {
                var limit = appSettings.Get<LimitGroup>(key);
                if (limit != null)
                {
                    return limit;
                }
            }

            if (log.IsDebugEnabled)
            {
                log.Debug($"No matching config values found for {keys.ToCsv()}");
            }

            return null;
        }

        private LimitGroup GetRequestLimits(IRequest request)
        {
            var requestKeys = keyGenerator.GetConfigKeysForRequest(request);
            var requestLimits = GetConfigLimit(requestKeys.ToArray());
            return requestLimits;
        }

        private LimitGroup GetUserLimits(IRequest request)
        {
            var userKey = keyGenerator.GetConfigKeysForUser(request);
            var userLimit = GetConfigLimit(userKey.ToArray());
            return userLimit;
        }
    }
}