// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Tests.Utilities
{
    using FakeItEasy;
    using FluentAssertions;
    using Ploeh.AutoFixture.Xunit2;
    using Redis.Utilities;
    using ServiceStack.Redis;
    using Xunit;

    public class LuaScriptHelpersTests
    {
        [Fact]
        public void GetLuaScript_ReturnsScript()
        {
            var script = LuaScriptHelpers.GetLuaScript();
            script.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void RegisterLuaScript_GetsClient()
        {
            var clientManager = A.Fake<IRedisClientsManager>();

            LuaScriptHelpers.RegisterLuaScript(clientManager);

            A.CallTo(() => clientManager.GetClient()).MustHaveHappened();
        }

        [Fact]
        public void RegisterLuaScript_LoadsScript()
        {
            var clientManager = A.Fake<IRedisClientsManager>();
            var client = A.Fake<IRedisClient>();

            A.CallTo(() => clientManager.GetClient()).Returns(client);

            var resourceScript = LuaScriptHelpers.GetLuaScript();
            LuaScriptHelpers.RegisterLuaScript(clientManager);

            A.CallTo(() => client.LoadLuaScript(resourceScript)).MustHaveHappened();
        }

        [Theory, InlineAutoData]
        public void RegisterLuaScript_ReturnsSha1(string sha1)
        {
            var clientManager = A.Fake<IRedisClientsManager>();
            var client = A.Fake<IRedisClient>();

            A.CallTo(() => clientManager.GetClient()).Returns(client);
            A.CallTo(() => client.LoadLuaScript(A<string>.Ignored)).Returns(sha1);

            var result = LuaScriptHelpers.RegisterLuaScript(clientManager);

            result.Should().Be(sha1);
        }
    }
}