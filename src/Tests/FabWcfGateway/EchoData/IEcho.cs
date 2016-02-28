using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace EchoApp
{
    [ServiceContract]
    public interface IEcho
    {
        [OperationContract]
        string Echo(string text);
    }
}
