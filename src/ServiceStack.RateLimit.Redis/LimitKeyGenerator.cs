﻿// This Source Code Form is subject to the terms of the Mozilla Public
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
        private const string DefaultConfigKey = "lmt:default";
        private const string DefaultUserConfigKey = "lmt:usr:default";
        private readonly ILog log = LogManager.GetLogger(typeof(LimitKeyGenerator));
         
        // This is how we will generate the key that is used to lookup the LimitProvider
        public virtual IEnumerable<string> GetConfigKeysForRequest(IRequest request)
        {
            string userId = GetConsumerId(request);
            string requestId = GetRequestId(request);

            // Build up a list of all keys in order of precedence
            string userRequestKey = $"lmt:{requestId}:{userId}";
            string requestKey = $"lmt:{requestId}";

            return new[] { userRequestKey, requestKey, DefaultConfigKey };
        }

        public virtual IEnumerable<string> GetConfigKeysForUser(IRequest request)
        {
            string userId = GetConsumerId(request);

            string userKey = $"lmt:usr:{userId}";
            return new[] { userKey, DefaultUserConfigKey };
        }

        public virtual string GetRequestId(IRequest request)
        {
            return request.OperationName?.ToLowerInvariant();
        }

        public virtual string GetConsumerId(IRequest request)
        {
            // HERE: IF this is un as party of hte PreRequestFilter then we won't know who they are yet so will needs some sort of whitelisrt to be
            // able to allow them to authenticate as it looks like the authentication is done once the DTO is deserialized so maybe it's just that the
            // basic auth plugin that i'm using sucks?

            IAuthSession userSession = request.GetSession();

            // TODO This will need more love to authorize user rather than just verify authentication (not necessarily here but in general)
            if (!(userSession?.IsAuthenticated ?? false))
            {
                log.Error($"User {userSession?.UserName ?? "<unknown>"} not authenticated for request {request.AbsoluteUri}");
                throw new AuthenticationException("You must be authenticated to access this service");
            }

            return userSession.UserAuthId?.ToLowerInvariant();
        }
    }
}