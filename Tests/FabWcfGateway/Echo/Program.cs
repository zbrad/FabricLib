namespace EchoApp
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            ZBrad.FabricLib.Utilities.Utility.Register<EchoService>();
        }
    }
}
