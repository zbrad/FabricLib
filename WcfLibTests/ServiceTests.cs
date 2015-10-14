using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZBrad.WcfLib;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace WcfLibTests
{
    [TestClass]
    public class ServiceTests
    {
        [TestMethod]
        public void TcpDefault()
        {
            var path = new Uri("Tcp://localhost:8088/");
            var foo = new Foo();
            var service = new TcpService();
            service.Initialize(path, foo);
            service.StartAsync().Wait();
            Assert.IsTrue(service.IsListening);

            TcpClient<IFoo> client;
            var clientCreateResult = TcpClient<IFoo>.TryCreate(path, out client);
            Assert.IsTrue(clientCreateResult);

            client.Instance.SetName("bar");
            Assert.AreEqual<string>("bar", foo.Name);
        }


        // Hello World with the Routing Service
        // https://msdn.microsoft.com/en-us/library/dd795218%28v=vs.110%29.aspx

        [TestMethod]
        public void RoutingDefault()
        {
            var routerPath = new Uri("net.tcp://localhost:9000/gateway");

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
            Assert.AreEqual("routed bar", service1.Instance.Name);
        }
    }
}
