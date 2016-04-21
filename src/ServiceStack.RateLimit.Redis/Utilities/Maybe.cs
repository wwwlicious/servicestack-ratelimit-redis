using System;

namespace ServiceStack.RateLimit.Redis.Utilities
{
    /// <summary>
    /// Used to specify that a class may return null object in certain instances
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>This is a naive implementation as it's not widely used in this instance</remarks>
    public struct Maybe<T>
        where T : class
    {
        private readonly T value;

        public T Value
        {
            get
            {
                if (!HasValue)
                    throw new InvalidOperationException("Nullable object must have a value");

                return value;
            }
        }

        public bool HasValue => value != null;

        public Maybe(T value)
        {
            this.value = value;
        }
    }
}
