// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Interfaces
{
    using System.Collections.Generic;
    using Web;

    public interface ILimitKeyGenerator
    {
        /// <summary>
        /// For given request will generate a collection of config keys to check, in order of precedence.
        /// User/request -> user all requests -> request -> default
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        IEnumerable<string> GetConfigKeysForRequest(IRequest request);

        /// <summary>
        /// For given request will generate the config key to check for user limit (across ALL requests)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        IEnumerable<string> GetConfigKeysForUser(IRequest request);

        /// <summary>
        /// Generates a unique identifier for specified request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        string GetRequestId(IRequest request);

        /// <summary>
        /// Generate a unique consumer identifier for specified request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        string GetConsumerId(IRequest request);
    }
}