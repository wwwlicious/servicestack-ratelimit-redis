// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this 
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.RateLimit.Redis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Configuration;
    using FakeItEasy;
    using FluentAssertions;
    using Interfaces;
    using Redis.Models;
    using Testing;
    using Xunit;

    [Collection("RateLimitFeature")]
    public class AppSettingsLimitProviderTests
    {
        public AppSettingsLimitProviderTests()
        {
            keyGenerator = A.Fake<ILimitKeyGenerator>();
            appSetting = A.Fake<IAppSettings>();

            limitProvider = new AppSettingsLimitProvider(keyGenerator, appSetting);
        }

        private readonly AppSettingsLimitProvider limitProvider;
        private readonly ILimitKeyGenerator keyGenerator;
        private readonly IAppSettings appSetting;

        [Theory]
        [AutoData]
        public void GetRateLimitScriptId_ReturnsAppSetting(string scriptId)
        {
            A.CallTo(() => appSetting.GetString(LimitProviderConstants.ScriptKey)).Returns(scriptId);

            var result = limitProvider.GetRateLimitScriptId();

            result.Should().Be(scriptId);
        }

        [Theory]
        [InlineAutoData]
        public void GetLimits_ReturnsRequestLimits_FromConfig(IEnumerable<string> requestKeys, LimitGroup limitGroup)
        {
            var mockHttpRequest = new MockHttpRequest();

            A.CallTo(() => keyGenerator.GetConfigKeysForRequest(mockHttpRequest)).Returns(requestKeys);
            A.CallTo(() => appSetting.Get<LimitGroup>(requestKeys.First())).Returns(limitGroup);

            var limits = limitProvider.GetLimits(mockHttpRequest);

            limits.Request.Should().Be(limitGroup);
        }

        [Theory]
        [InlineAutoData]
        public void GetLimits_ReturnsUserLimits_FromConfig(IEnumerable<string> userKeys, LimitGroup limitGroup)
        {
            var mockHttpRequest = new MockHttpRequest();

            A.CallTo(() => keyGenerator.GetConfigKeysForUser(mockHttpRequest)).Returns(userKeys);
            A.CallTo(() => appSetting.Get<LimitGroup>(userKeys.First())).Returns(limitGroup);

            var limits = limitProvider.GetLimits(mockHttpRequest);

            limits.User.Should().Be(limitGroup);
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfAppSettingNull()
        {
            Action action = () => new AppSettingsLimitProvider(A.Fake<ILimitKeyGenerator>(), null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfKeyGeneratorNull()
        {
            Action action = () => new AppSettingsLimitProvider(null, A.Fake<IAppSettings>());
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