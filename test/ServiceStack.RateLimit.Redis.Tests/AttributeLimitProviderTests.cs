// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using AutoFixture.Xunit2;
using FakeItEasy;
using FluentAssertions;
using ServiceStack.Configuration;
using ServiceStack.Testing;
using Xunit;

namespace ServiceStack.RateLimit.Redis.Tests
{
    public class AttributeLimitProviderTests : IClassFixture<AppHostFixture>
    {
        public AttributeLimitProviderTests()
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
            const string scriptKey = "script:ratelimit";
            A.CallTo(() => appSetting.GetString(scriptKey)).Returns(scriptId);

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
    }
}