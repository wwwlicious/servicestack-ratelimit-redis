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
            appSetting = new SimpleAppSettings();

            limitProvider = new AppSettingsLimitProvider(keyGenerator, appSetting);
        }

        private readonly AppSettingsLimitProvider limitProvider;
        private readonly ILimitKeyGenerator keyGenerator;
        private readonly IAppSettings appSetting;

        [Theory]
        [AutoData]
        public void GetRateLimitScriptId_ReturnsAppSetting(string scriptId)
        {
            appSetting.Set(LimitProviderConstants.ScriptKey, scriptId);

            limitProvider.GetRateLimitScriptId().Should().Be(scriptId);
        }

        [Theory]
        [InlineAutoData]
        public void GetLimits_ReturnsRequestLimits_FromConfig(string[] requestKeys, LimitGroup limitGroup)
        {
            var request = new MockHttpRequest();
            appSetting.Set(requestKeys.First(), limitGroup);

            A.CallTo(() => keyGenerator.GetConfigKeysForRequest(request)).Returns(requestKeys);
            
            limitProvider.GetLimits(request).Request.Should().BeEquivalentTo(limitGroup);
        }

        [Theory]
        [InlineAutoData]
        public void GetLimits_ReturnsUserLimits_FromConfig(IEnumerable<string> userKeys, LimitGroup limitGroup)
        {
            var request = new MockHttpRequest();
            appSetting.Set(userKeys.First(), limitGroup);

            A.CallTo(() => keyGenerator.GetConfigKeysForUser(request)).Returns(userKeys);

            limitProvider.GetLimits(request).User.Should().BeEquivalentTo(limitGroup);
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
            Action action = () => new AppSettingsLimitProvider(null, new SimpleAppSettings());
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
    }
}