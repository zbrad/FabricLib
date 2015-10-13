using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ZBrad.WcfLib
{
    public interface IWcfService : IStartable
    {
        Binding Binding { get; }
        ServiceHost Host { get; }
        bool IsListening { get; }
        Uri Uri { get; }

        void Initialize(ServiceHost host);
        void Initialize(Uri path, object instance);
    }
}