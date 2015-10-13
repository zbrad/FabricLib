using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Wcf
{
    /// <summary>
    /// specifies partition to by used by client
    /// </summary>
    public class Partition : IPartition
    {
        public const string KeyHeader = "PartitionKey";

        Header header;

        /// <summary>
        /// constructs a <c>Partition</c> for client use
        /// </summary>
        /// <param name="key">partition key to be used</param>
        public Partition(string key)
        {
            header = new Header(key);
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(header);
        }

        #region noop
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
        #endregion

        class Header : IClientMessageInspector
        {
            MessageHeader header;

            public Header(string value)
            {
                this.header = MessageHeader.CreateHeader(KeyHeader, string.Empty, value);
            }

            public object BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                request.Headers.Add(header);
                return null;
            }

            #region noop
            public void AfterReceiveReply(ref Message reply, object correlationState)
            {
            }
            #endregion
        }
    }

}
