using Informatikprojekt_DotNetVersion.Shared.Util;

namespace Informatikprojekt_DotNetVersion
{
    public class ClientProgram
    {
        public static void Programm(string[] args)
        {
            Config config = Config.readConfigFromCLIArgs(args);
            Client client = new Client(config.hostIP, config.username, config.password, config.port, "LaptopClient");
        }
    }
}