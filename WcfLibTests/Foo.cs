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
    [ServiceContract]
    public interface IFoo
    {
        [OperationContract]
        string GetName();

        [OperationContract]
        void SetName(string name);

        [OperationContract]
        int GetValue();

        [OperationContract]
        void SetValue(int value);
    }

    public class FooClient : ClientBase<IFoo>
    {
        public FooClient(Binding b, EndpointAddress e) : base(b, e)
        { }
    }

    public class Foo : IFoo
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public string GetName()
        {
            return this.Name;
        }

        public int GetValue()
        {
            return this.Value;
        }

        public void SetName(string name)
        {
            this.Name = name;
        }

        public void SetValue(int value)
        {
            this.Value = value;
        }
    }
}
