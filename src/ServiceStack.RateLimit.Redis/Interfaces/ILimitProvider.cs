// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Interfaces
{
    using Models;
    using Web;

    public interface ILimitProvider
    {
        /// <summary>
        /// Returns limits for specified request. If no limits found default is returned.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Limits GetLimits(IRequest request);

        /// <summary>
        /// Returns the Id of the script Lua script to use for rate limiting
        /// </summary>
        /// <returns></returns>
        string GetRateLimitScriptId();
    }
}
