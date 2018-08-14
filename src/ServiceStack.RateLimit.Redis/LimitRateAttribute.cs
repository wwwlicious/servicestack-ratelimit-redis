// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this 
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.RateLimit.Redis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Web;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class LimitRateAttribute : RequestFilterAttribute
    {
        public const string RequestItemName = "RateLimit.Limits";

        public LimitRateAttribute() : this(LimitType.PerRequest, LimitProviderConstants.DefaultPerMinute,
            (int) RatePeriod.PerMinute)
        {
        }

        public LimitRateAttribute(LimitType type) : this(type, LimitProviderConstants.DefaultPerMinute,
            (int) RatePeriod.PerMinute)
        {
        }

        public LimitRateAttribute(int limit, int seconds) : this(LimitType.PerRequest, limit, seconds)
        {
        }

        public LimitRateAttribute(int limit, RatePeriod period = RatePeriod.PerMinute) : this(LimitType.PerRequest,
            limit, (int) period)
        {
        }

        public LimitRateAttribute(LimitType type, int limit, RatePeriod period = RatePeriod.PerMinute) : this(type,
            limit, (int) period)
        {
        }

        public LimitRateAttribute(LimitType type, int limit, int seconds)
        {
            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }
            if (seconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            Limit = limit;
            Seconds = seconds;
            Type = type;
        }

        public int Limit { get; }

        public int Seconds { get; }

        public LimitType Type { get; }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (!(req.Items.GetValueOrDefault(RequestItemName) is Limits limits))
            {
                req.Items[RequestItemName] = new Limits
                {
                    Request = Type == LimitType.PerRequest
                        ? new LimitGroup
                        {
                            Limits = new List<LimitPerSecond>
                            {
                                this.ConvertTo<LimitPerSecond>()
                            }
                        }
                        : null,
                    User = Type == LimitType.PerUser
                        ? new LimitGroup
                        {
                            Limits = new List<LimitPerSecond>
                            {
                                this.ConvertTo<LimitPerSecond>()
                            }
                        }
                        : null
                };
            }
            else
            {
                var group = Type == LimitType.PerRequest ? limits.Request : limits.User;
                var limit = (group ?? new LimitGroup()).Limits.Safe().ToList();
                limit.Add(this.ConvertTo<LimitPerSecond>());
                group.Limits = limit;
            }
        }
    }

    public enum RatePeriod
    {
        PerSecond = 1,
        PerMinute = 60,
        PerHour = 3600,
        PerDay = 86400
    }

    public enum LimitType
    {
        PerUser = 0,
        PerRequest = 1
    }
}