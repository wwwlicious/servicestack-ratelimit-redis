// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this 
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using System.Collections.Generic;
    using FakeItEasy;
    using FluentAssertions;
    using Redis.Models;
    using Web;
    using Xunit;

    public class LimitRateAttributeTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Ctor_ThrowsArgumentNullException_IfLimitZeroOrNegative(int limit)
        {
            Action action = () => new LimitRateAttribute(limit, 1);
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Ctor_ThrowsArgumentNullException_IfSecondsZeroOrNegative(int seconds)
        {
            Action action = () => new LimitRateAttribute(1, seconds);
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Ctor_SetsDefaultLimitAndRate()
        {
            var attribute = new LimitRateAttribute();

            attribute.Limit.Should().Be(LimitProviderConstants.DefaultPerMinute);
            attribute.Seconds.Should().Be((int) RatePeriod.PerMinute);
            attribute.Type.Should().Be(LimitType.PerRequest);
        }

        [Fact]
        public void Ctor_SetsLimitAndRate_IfOnlyType()
        {
            var attribute = new LimitRateAttribute(LimitType.PerUser);

            attribute.Limit.Should().Be(LimitProviderConstants.DefaultPerMinute);
            attribute.Seconds.Should().Be((int) RatePeriod.PerMinute);
            attribute.Type.Should().Be(LimitType.PerUser);
        }

        [Fact]
        public void Ctor_SetsLimitAndRate_IfRatePeriod()
        {
            var attribute = new LimitRateAttribute(1, RatePeriod.PerSecond);

            attribute.Limit.Should().Be(1);
            attribute.Seconds.Should().Be((int) RatePeriod.PerSecond);
        }

        [Fact]
        public void Ctor_SetsLimitAndRate_IfTypeAndRatePeriod()
        {
            var attribute = new LimitRateAttribute(LimitType.PerUser, 1, RatePeriod.PerSecond);

            attribute.Limit.Should().Be(1);
            attribute.Seconds.Should().Be((int) RatePeriod.PerSecond);
            attribute.Type.Should().Be(LimitType.PerUser);
        }

        [Fact]
        public void Execute_AddsFirstRequestLimit_IfNoLimits()
        {
            var request = A.Fake<IRequest>();
            var response = A.Fake<IResponse>();
            var attribute = new LimitRateAttribute(LimitType.PerRequest, 6, 45);

            attribute.Execute(request, response, null);

            request.Items[LimitRateAttribute.RequestItemName].Should().BeEquivalentTo(new Limits
            {
                Request = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 45, Limit = 6}
                    }
                },
                User = null
            });
        }

        [Fact]
        public void Execute_AddsFirstUserLimit_IfNoLimits()
        {
            var request = A.Fake<IRequest>();
            var response = A.Fake<IResponse>();
            var attribute = new LimitRateAttribute(LimitType.PerUser, 6, 45);

            attribute.Execute(request, response, null);

            request.Items[LimitRateAttribute.RequestItemName].Should().BeEquivalentTo(new Limits
            {
                Request = null,
                User = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 45, Limit = 6}
                    }
                }
            });
        }

        [Fact]
        public void Execute_AddsNextRequestLimit_IfRequestLimits()
        {
            var request = A.Fake<IRequest>();
            request.Items.Add(LimitRateAttribute.RequestItemName, new Limits
            {
                Request = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1}
                    }
                }
            });
            var response = A.Fake<IResponse>();
            var attribute = new LimitRateAttribute(LimitType.PerRequest, 6, 45);

            attribute.Execute(request, response, null);

            request.Items[LimitRateAttribute.RequestItemName].Should().BeEquivalentTo(new Limits
            {
                Request = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1},
                        new LimitPerSecond {Seconds = 45, Limit = 6}
                    }
                }
            });
        }

        [Fact]
        public void Execute_AddsNextUserLimit_IfBothLimits()
        {
            var request = A.Fake<IRequest>();
            request.Items.Add(LimitRateAttribute.RequestItemName, new Limits
            {
                Request = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1}
                    }
                },
                User = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1}
                    }
                }
            });
            var response = A.Fake<IResponse>();
            var attribute = new LimitRateAttribute(LimitType.PerUser, 6, 45);

            attribute.Execute(request, response, null);

            request.Items[LimitRateAttribute.RequestItemName].Should().BeEquivalentTo(new Limits
            {
                Request = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1}
                    }
                },
                User = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1},
                        new LimitPerSecond {Seconds = 45, Limit = 6}
                    }
                }
            });
        }

        [Fact]
        public void Execute_AddsNextUserLimit_IfUserLimits()
        {
            var request = A.Fake<IRequest>();
            request.Items.Add(LimitRateAttribute.RequestItemName, new Limits
            {
                Request = null,
                User = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1}
                    }
                }
            });
            var response = A.Fake<IResponse>();
            var attribute = new LimitRateAttribute(LimitType.PerUser, 6, 45);

            attribute.Execute(request, response, null);

            request.Items[LimitRateAttribute.RequestItemName].Should().BeEquivalentTo(new Limits
            {
                User = new LimitGroup
                {
                    Limits = new List<LimitPerSecond>
                    {
                        new LimitPerSecond {Seconds = 1, Limit = 1},
                        new LimitPerSecond {Seconds = 45, Limit = 6}
                    }
                }
            });
        }
    }
}