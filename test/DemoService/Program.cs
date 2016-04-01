// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace DemoService
{
    using System;
    using System.Diagnostics;
    using ServiceStack;
    using ServiceStack.Text;

    class Program
    {
        static void Main(string[] args)
        {
            var serviceUrl = "http://127.0.0.1:8090/";
            new AppHost(serviceUrl).Init().Start("http://*:8090/");
            $"ServiceStack SelfHost listening at {serviceUrl} ".Print();
            Process.Start(serviceUrl);

            Console.ReadLine();
        }
    }

    public class DemoService : Service
    {
        public object Any(DemoRequest demoRequest)
        {
            return new DemoResponse { Message = "Response from Demo Service" };
        }
    }

    [Authenticate]
    public class DemoRequest : IReturn<DemoResponse>
    {
    }

    public class DemoResponse
    {
        public string Message { get; set; }
    }
}
