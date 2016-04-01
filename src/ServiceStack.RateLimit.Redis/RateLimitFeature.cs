// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    using System;
    using System.Runtime.Serialization;
    using Headers;
    using Interfaces;
    using Logging;
    using Models;
    using ServiceStack;
    using ServiceStack.Redis;
    using Text;
    using Utilities;
    using Web;

    public class RateLimitFeature : IPlugin
    {
        /// <summary>
        /// Default header to use for uniquely identifying a request. Default "x-mac-requestid"
        /// </summary>
        public static string CorrelationIdHeader { get; set; } = "x-mac-requestid";

        /// <summary>
        /// Function for customising how request correlation ids are extracted from request
        /// </summary>
        public Func<IRequest, string> CorrelationIdExtractor { get; set; }

        /// <summary>
        /// Message returned if limit has been reached. Default "Too Many Requests"
        /// </summary>
        public string StatusDescription { get; set; } = "Too many requests.";

        /// <summary>
        /// Status code returned if limit has been reached. Default 429 (Too Many Requests)
        /// </summary>
        public int LimitStatusCode { get; set; } = 429;

        public ILimitProvider LimitProvider { get; set; }
        public ILimitKeyGenerator KeyGenerator { get; set; }

        private string scriptSha1;

        private readonly IRedisClientsManager redisClientsManager;
        
        private readonly ILog log = LogManager.GetLogger(typeof (RateLimitFeature));

        public RateLimitFeature(IRedisClientsManager redisClientsManager)
        {
            redisClientsManager.ThrowIfNull(nameof(redisClientsManager));

            this.redisClientsManager = redisClientsManager;
        }

        public void Register(IAppHost appHost)
        {
            EnsureDependencies(appHost);

            appHost.GlobalRequestFilters.Add(ProcessRequest);
        }

        public virtual void ProcessRequest(IRequest request, IResponse response, object obj)
        {
            var limits = LimitProvider.GetLimits(request);

            if (limits == null)
            {
                // No limits for request, continue
                log.Warn($"No limits found for request {request.AbsoluteUri}");
                return;
            }

            var rateLimitResult = GetLimitResult(request, limits);
            ProcessResult(response, rateLimitResult);
        }

        private static void SetLimitHeaders(IResponse response, RateLimitResult result)
        {
            var headerResults = RateLimitHeader.Create(result?.Results);

            var excludeTypeInfo = JsConfig.ExcludeTypeInfo;
            foreach (var header in headerResults)
            {
                JsConfig.ExcludeTypeInfo = true;
                response.AddHeader(header.HeaderName, header.Limits.ToJson());
            }

            JsConfig.ExcludeTypeInfo = excludeTypeInfo;
        }

        private void ProcessResult(IResponse response, RateLimitResult rateLimitResult)
        {
            SetLimitHeaders(response, rateLimitResult);

            // NOTE By default we return an empty RateLimitResult which will have an 'Access' value of null.. which we default to true. Is this correct?
            if (!rateLimitResult?.Access ?? true)
            {
                if (log.IsDebugEnabled)
                {
                    var request = response.Request;
                    log.Debug(
                        $"Rate limit exceeded for {request.AbsoluteUri}, user {KeyGenerator.GetConsumerId(request)}. Returning status code: {LimitStatusCode}");
                }
                response.StatusCode = LimitStatusCode;
                response.StatusDescription = StatusDescription;
                response.Close();
            }
        }

        private RateLimitResult GetLimitResult(IRequest request, Limits limits)
        {
            string consumerId = KeyGenerator.GetConsumerId(request);
            string requestId = KeyGenerator.GetRequestId(request);

            string args = GetLuaArgs(limits, request);

            using (var client = redisClientsManager.GetClient())
            {
                RedisText result = null;
                try
                {
                    // Call lua script to get current hit-rate and over access/no-access
                    result = client.ExecLuaSha(GetSha1(), new[] { consumerId, requestId }, new[] { args });

                    var rateLimitResult = result.Text.FromJson<RateLimitResult>();
                    return rateLimitResult;
                }
                catch (RedisResponseException e)
                {
                    log.Error($"Error executing rate-limit Lua script. Called with {args}", e);
                }
                catch (SerializationException e)
                {
                    log.Error(
                        $"Error serialising rate-limit Lua script return to RateLimitResult. Result: {result?.Text}. Called with {args}",
                        e);
                }
                catch (Exception e)
                {
                    log.Error($"Error getting rate-limit result from Redis. Called with {args}", e);
                }
            }

            return new RateLimitResult();
        }

        private string GetLuaArgs(Limits limits, IRequest request)
        {
            var args = new { Time = limits, Stamp = SecondsFromUnixTime(), RequestId = GetRequestCorrelationId(request) };
            return args.ToJson();
        }

        private string GetRequestCorrelationId(IRequest request)
        {
            return CorrelationIdExtractor == null ? request.GetRequestCorrelationId() : CorrelationIdExtractor(request);
        }

        private static int SecondsFromUnixTime()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (int) timeSpan.TotalSeconds;
        }

        private string GetSha1()
        {
            var scriptFromConfig = LimitProvider.GetRateLimitScriptId();
            if (!string.IsNullOrWhiteSpace(scriptFromConfig))
            {
                log.Debug($"Got Lua script sha1 {scriptFromConfig} from config");
                return scriptFromConfig;
            }

            if (string.IsNullOrEmpty(scriptSha1))
            {
                log.Info("Registering Lua rate limiting script");
                scriptSha1 = LuaScriptHelpers.RegisterLuaScript(redisClientsManager);
            }
            return scriptSha1;
        }

        private void EnsureDependencies(IAppHost appHost)
        {           
            if (KeyGenerator == null)
                KeyGenerator = new LimitKeyGenerator();

            if (LimitProvider == null)
                LimitProvider = new LimitProviderBase(KeyGenerator, appHost.AppSettings);
        }
    }
}