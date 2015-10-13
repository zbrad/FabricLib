using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZBrad.WcfLib;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using WcfLibTests;

namespace WcfLibConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //SimpleConnect();

            Routing();
        }

        static void SimpleConnect()
        {
            var foo = FooService.Create("net.tcp://localhost:8080/");
            foo.Service.StartAsync().Wait();

            TcpClient<IFoo> client;
            var clientCreateResult = TcpClient<IFoo>.TryCreate(foo.Uri, out client);
            client.Instance.SetName("bar");

            // validate instance
            if (!foo.Instance.Name.Equals("bar"))
                throw new ApplicationException("names did not match");
        }

        // Hello World with the Routing Service
        // https://msdn.microsoft.com/en-us/library/dd795218%28v=vs.110%29.aspx

        static void Routing()
        {
            var routerPath = new Uri("net.tcp://localhost:9000/");

            var service1 = FooService.Create("net.tcp://localhost:8080/bar");
            service1.Service.StartAsync().Wait();
            var service2 = FooService.Create("net.tcp://localhost:8081/baz");
            service2.Service.StartAsync().Wait();

            var resolver = new TestResolver(service1, service2);
            var gateway = new TcpRouter();

            gateway.Initialize(routerPath, resolver);
            gateway.Service.StartAsync().Wait();


            var clientPath = new Uri("net.tcp://localhost/bar");
            TcpClient<IFoo> client;
            var clientCreateResult = TcpClient<IFoo>.TryCreate(clientPath, routerPath, out client);
            client.Instance.SetName("routed bar");

            // verify it's set
            if (!service1.Instance.Name.Equals("routed bar"))
                throw new ApplicationException("name not set");
        }
    }
}
