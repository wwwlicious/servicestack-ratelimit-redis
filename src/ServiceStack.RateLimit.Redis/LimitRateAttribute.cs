using System;

namespace ServiceStack.RateLimit.Redis
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class LimitRateAttribute : AttributeBase
    {
        public LimitRateAttribute()
        {
            Limit = LimitProviderConstants.DefaultPerMinute;
            Seconds = 60;
        }

        public int Limit { get; set; }

        public int Seconds { get; set; }
    }
}