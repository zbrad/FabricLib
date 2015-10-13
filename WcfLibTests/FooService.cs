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

namespace WcfLibTests
{
    public class FooService
    {
        public Uri Uri;
        public Foo Instance;
        public TcpService Service;

        public static FooService Create(string s)
        {
            var foo = new FooService();
            foo.Uri = new Uri(s);
            foo.Instance = new Foo();
            foo.Service = new TcpService();
            foo.Service.Initialize(foo.Uri, foo.Instance);
            return foo;
        }
    }
}