using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Routing;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.WcfLib
{
    //public interface IResolver<T> : IDispatchMessageInspector, IEndpointBehavior where T : Filter
    public interface IResolver : IDispatchMessageInspector, IEndpointBehavior
    {
        IRouter Router { get; }
        void Initialize(IRouter retry);
        //Task<T> UpdateFilter(Message request, T oldfilter);
        //Task<T> CreateFilter(Message request);
    }
}
