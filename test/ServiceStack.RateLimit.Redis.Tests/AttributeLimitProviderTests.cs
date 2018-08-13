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
            appSetting = A.Fake<IAppSettings>();

            limitProvider = new AttributeLimitProvider(appSetting);
        }

        private readonly AttributeLimitProvider limitProvider;
        private readonly IAppSettings appSetting;

        [Theory]
        [AutoData]
        public void GetRateLimitScriptId_ReturnsAppSetting(string scriptId)
        {
            A.CallTo(() => appSetting.GetString(LimitProviderConstants.ScriptKey)).Returns(scriptId);

            var result = limitProvider.GetRateLimitScriptId();

            result.Should().Be(scriptId);
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
            var limits = limitProvider.GetLimits(new MockHttpRequest());

            limits.Should().NotBeNull();
        }

        [Fact]
        public void GetLimits_ReturnsDefaultRequestLimits_IfNoneFound()
        {
            var limits = limitProvider.GetLimits(new MockHttpRequest());

            limits.Request.Limits.Count().Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetLimits_ReturnsNoUserLimits_IfNoneFound()
        {
            var limits = limitProvider.GetLimits(new MockHttpRequest());

            limits.User.Should().BeNull();
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

            var limits = limitProvider.GetLimits(request);

            limits.Request.Should().Be(requestLimits);
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

            var limits = limitProvider.GetLimits(request);

            limits.User.Should().Be(userLimits);
        }
    }
}