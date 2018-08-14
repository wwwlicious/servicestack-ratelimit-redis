// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this 
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Configuration;
    using FakeItEasy;
    using FluentAssertions;
    using Redis.Models;
    using Testing;
    using Xunit;

    [Collection("RateLimitFeature")]
    public class AttributeLimitProviderTests
    {
        public AttributeLimitProviderTests(RateLimitAppHostFixture fixture)
        {
            appSetting = new SimpleAppSettings();

            limitProvider = new AttributeLimitProvider(appSetting);
        }

        private readonly AttributeLimitProvider limitProvider;
        private readonly IAppSettings appSetting;

        [Theory]
        [AutoData]
        public void GetRateLimitScriptId_ReturnsAppSetting(string scriptId)
        {
            appSetting.Set(LimitProviderConstants.ScriptKey, scriptId);

            limitProvider.GetRateLimitScriptId().Should().Be(scriptId);
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfAppSettingNull()
        {
            Action action = () => new AttributeLimitProvider(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetLimits_AlwaysReturnsObject()
        {
            limitProvider.GetLimits(new MockHttpRequest()).Should().NotBeNull();
        }

        [Fact]
        public void GetLimits_ReturnsDefaultRequestLimits_IfNoneFound()
        {
            limitProvider.GetLimits(new MockHttpRequest()).Request.Limits.Count().Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetLimits_ReturnsNoUserLimits_IfNoneFound()
        {
            limitProvider.GetLimits(new MockHttpRequest()).User.Should().BeNull();
        }

        [Fact]
        public void GetLimits_ReturnsRequestLimitsFromRequestItems_IfFound()
        {
            var requestLimits = new LimitGroup();
            var request = new MockHttpRequest
            {
                Items =
                {
                    [LimitRateAttribute.RequestItemName] = new Limits
                    {
                        Request = requestLimits
                    }
                }
            };

            limitProvider.GetLimits(request).Request.Should().Be(requestLimits);
        }

        [Fact]
        public void GetLimits_ReturnsUserLimitsFromRequestItems_IfFound()
        {
            var userLimits = new LimitGroup();
            var request = new MockHttpRequest
            {
                Items =
                {
                    [LimitRateAttribute.RequestItemName] = new Limits
                    {
                        User = userLimits
                    }
                }
            };

            limitProvider.GetLimits(request).User.Should().Be(userLimits);
        }
    }
}