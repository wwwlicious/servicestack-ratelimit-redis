// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.RateLimit.Redis.Utilities
{
    using System.IO;
    using System.Reflection;
    using ServiceStack.Redis;

    public static class LuaScriptHelpers
    {
        public static string GetLuaScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ServiceStack.RateLimit.Redis.Scripts.RateLimitHash.lua";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        public static string RegisterLuaScript(IRedisClientsManager clientManager)
        {
            using (var client = clientManager.GetClient())
            {
                string scriptSha1 = client.LoadLuaScript(GetLuaScript());
                return scriptSha1;
            }
        }
    }
}