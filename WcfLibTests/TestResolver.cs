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
    public class TestResolver : Resolver
    {
        List<FooService> services = new List<FooService>();

        // require first service, but can also handle additional ones
        public TestResolver(FooService first, params FooService[] additional)
        {
            this.services.Add(first);
            if (additional != null)
                this.services.AddRange(additional);
        }

        public override Task<Filter> CreateFilter(Message request)
        {
            TestFilter filter = null;
            // resolve the "To" address as the matching entry
            foreach (var f in services)
            {
                if (f.Service.Uri.AbsolutePath == request.Headers.To.AbsolutePath)
                {
                    filter = new TestFilter();
                    filter.Initialize(request.Headers.To, new Uri[] { f.Uri });
                    break;
                }
            }

            return Task.FromResult<Filter>(filter);
        }

        public override Task<Filter> UpdateFilter(Message request, Filter oldfilter)
        {
            var filter = new TestFilter();
            filter.Initialize(request.Headers.To, new Uri[] { oldfilter.Endpoints[0].Address.Uri });
            return Task.FromResult<Filter>(filter);
        }
    }
}
