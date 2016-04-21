// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    using System.Collections.Generic;
    using Auth;
    using Interfaces;
    using Logging;
    using ServiceStack;
    using Web;

    public class LimitKeyGenerator : ILimitKeyGenerator
    {
        public static string Delimiter = "/";
        public static string Prefix = "ss";

        private readonly string defaultConfigKey = GenerateKey("lmt", "default");
        private readonly string defaultUserConfigKey = GenerateKey("lmt", "usr", "default");

        private readonly ILog log = LogManager.GetLogger(typeof(LimitKeyGenerator));
         
        // This is how we will generate the key that is used to lookup the LimitProvider
        public virtual IEnumerable<string> GetConfigKeysForRequest(IRequest request)
        {
            string userId = GetConsumerId(request);
            string requestId = GetRequestId(request);

            // Build up a list of all keys in order of precedence
            string userRequestKey = GenerateKey("lmt", requestId, userId);
            string requestKey = GenerateKey("lmt", requestId);

            return new[] { userRequestKey, requestKey, defaultConfigKey };
        }

        public virtual IEnumerable<string> GetConfigKeysForUser(IRequest request)
        {
            string userId = GetConsumerId(request);

            string userKey = GenerateKey("lmt", "usr", userId);
            return new[] { userKey, defaultUserConfigKey };
        }

        public virtual string GetRequestId(IRequest request)
        {
            return request.OperationName?.ToLowerInvariant();
        }

        public virtual string GetConsumerId(IRequest request)
        {
            IAuthSession userSession = request.GetSession();

            // TODO This will need more love to authorize user rather than just verify authentication (not necessarily here but in general)
            if (!IsUserAuthenticated(userSession))
            {
                log.Error($"User {userSession?.UserName ?? "<unknown>"} not authenticated for request {request.AbsoluteUri}");
                throw new AuthenticationException("You must be authenticated to access this service");
            }

            return userSession.UserAuthId?.ToLowerInvariant();
        }

        private static bool IsUserAuthenticated(IAuthSession userSession)
        {
            return userSession?.IsAuthenticated ?? false;
        }

        private static string GenerateKey(params string[] keyParts)
        {
            var usablePrefix = string.IsNullOrWhiteSpace(Prefix) ? string.Empty : string.Concat(Prefix, Delimiter);

            return $"{usablePrefix}{string.Join(Delimiter, keyParts)}";
        }
    }
}
