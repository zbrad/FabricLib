using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZBrad.FabricLib.Wcf;

namespace EchoApp
{
    class Program
    {
        static string dest = "net.tcp://localhost:33001/EchoApp/EchoService/S/130886323977978280/";
        static void Main(string[] args)
        {
            TcpClient<IEcho> client;
            while (TcpClient<IEcho>.TryCreate(dest, out client))
            {
                bool isLive = true;
                while (isLive)
                {
                    try
                    {
                        //Console.Write("Send:");
                        //var line = Console.ReadLine();
                        var echo = client.Instance.Echo("test");
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
