using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZBrad.WcfLib;

namespace EchoApp
{
    class Program
    {
        static string dest = "net.tcp://localhost:33001/EchoApp/EchoService/S/130892330978654698/";
        static string via = "net.tcp://localhost:8080/EchoGateway/EchoGatewayService";
        static string service = "fabric:/EchoApp/EchoService";

        static void Main(string[] args)
        {
            TestVia();
        }

        static void TestDirect()
        {
            var destUri = new Uri(dest);
            TcpClient<IEcho> client;
            while (TcpClient<IEcho>.TryCreate(destUri, out client))
            {
                bool isLive = true;
                while (isLive)
                {
                    try
                    {
                        Console.Write("Send:");
                        var line = Console.ReadLine();
                        var echo = client.Instance.Echo(line);
                        Console.WriteLine("Received: " + echo);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: " + e.Message);
                        isLive = false;
                    }
                }
            }
        }

        static void TestVia()
        {
            var serviceUri = new Uri(service);
            var viaUri = new Uri(via);

            TcpClient<IEcho> client;
            while (TcpClient<IEcho>.TryCreate(serviceUri, viaUri, out client))
            {
                bool isLive = true;
                while (isLive)
                {
                    try
                    {
                        Console.Write("Send:");
                        var line = Console.ReadLine();
                        var echo = client.Instance.Echo(line);
                        Console.WriteLine("Received: " + echo);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: " + e.Message);
                        isLive = false;
                    }
                }
            }
        }
    }
}
