// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Utilities
{
    using System;

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
