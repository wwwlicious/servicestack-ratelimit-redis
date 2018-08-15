// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Headers;
    using Interfaces;
    using Logging;
    using Models;
    using ServiceStack;
    using ServiceStack.Configuration;
    using ServiceStack.OrmLite;
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

        /// <summary>
        /// Provides a list of limits per request
        /// </summary>
        public ILimitProvider[] LimitProviders { get; set; }

        /// <summary>
        /// Provides a variety of unique keys for requests.
        /// </summary>
        public ILimitKeyGenerator KeyGenerator { get; set; }
        
        public IAppSettings AppSettings { get; private set; }

        private string scriptSha1;

        private readonly IRedisClientsManager redisClientsManager;

        private readonly ILog log = LogManager.GetLogger(typeof (RateLimitFeature));

        public RateLimitFeature(IRedisClientsManager redisClientsManager)
        {
            this.redisClientsManager = redisClientsManager.ThrowIfNull(nameof(redisClientsManager));
        }

        public void Register(IAppHost appHost)
        {
            AppSettings = appHost.AppSettings;
            EnsureDependencies();
            appHost.GlobalRequestFilters.Add(ProcessRequest);
        }

        public virtual void ProcessRequest(IRequest request, IResponse response, object obj)
        {
            var reqLimits = new List<LimitPerSecond>();
            var userLimits = new List<LimitPerSecond>();
            foreach (var limitProvider in LimitProviders)
            {
                var limit = limitProvider.GetLimits(request);
                if(limit.Request != null) reqLimits.AddRange(limit.Request.Limits);
                if(limit.User != null) userLimits.AddRange(limit.User.Limits);
            }

            if (reqLimits.IsEmpty() && userLimits.IsEmpty())
            {
                // No limits for request, continue
                log.Debug($"No limits found for request {request.AbsoluteUri}");
                return;
            }

            var combinedLimits = new Limits
            {
                User = new LimitGroup { Limits = userLimits },
                Request = new LimitGroup { Limits = reqLimits }
            };
            
            var rateLimitResult = GetLimitResult(request, combinedLimits);
            ProcessResult(response, rateLimitResult);
        }

        private static void SetLimitHeaders(IResponse response, RateLimitResult result)
        {
            var headerResults = RateLimitHeader.Create(result?.Results);

            using (var config = JsConfig.BeginScope())
            {
                config.ExcludeTypeInfo = true;
                foreach (var header in headerResults)
                {
                    response.AddHeader(header.HeaderName, header.Limits.ToJson());
                }
            }
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
                    // Call lua script to get current hit-rate and overall access/no-access
                    result = client.ExecLuaSha(GetSha1(), new[] { consumerId, requestId }, new[] { args });
                    return result.Text.FromJson<RateLimitResult>();
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
            var scriptFromConfig = AppSettings.GetString(LimitProviderConstants.ScriptKey);
            if (!string.IsNullOrWhiteSpace(scriptFromConfig))
            {
                log.Debug($"Got Lua script sha1 {scriptFromConfig} from config");
                return scriptFromConfig;
            }

            if (string.IsNullOrEmpty(scriptSha1))
            {
                log.Info("Registering Lua rate limiting script");
                scriptSha1 = LuaScriptHelpers.RegisterLuaScript(redisClientsManager);
                AppSettings.Set(LimitProviderConstants.ScriptKey, scriptSha1);
            }
            return scriptSha1;
        }

        private void EnsureDependencies()
        {           
            if (KeyGenerator == null)
                KeyGenerator = new LimitKeyGenerator();

            if (LimitProviders.IsEmpty())
                LimitProviders = new ILimitProvider[]
                {
                    new AppSettingsLimitProvider(KeyGenerator, AppSettings),
                    //new AttributeLimitProvider()
                };
        }
    }
}