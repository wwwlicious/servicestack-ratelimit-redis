// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Models
{
    /// <summary>
    /// Represents a limit for a duration of time
    /// </summary>
    public class LimitDuration
    {
        /// <summary>
        /// Limit of how many requests per duration
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Time period limit refers to (seconds)
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// The precision for storing sliding expiry
        /// </summary>
        /// <remarks>This is currently not used</remarks>
        public int? Precision { get; set; }

        public override string ToString()
        {
            return $"{Limit},{Duration},{Precision ?? 0}";
        }
    }
}