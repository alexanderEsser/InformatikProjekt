using System;
using System.Collections.Generic;

namespace Informatikprojekt_DotNetVersion.Shared.Util
{
    public class Config
    {
        //@Parameter(names = {"-rh"}, description = "RabbitMQ host", required = true)
        public String hostIP;

        //@Parameter(names = {"-u"}, description = "RabbitMQ username", required = true)
        public String username;

        //@Parameter(names = {"-pa"}, description = "RabbitMQ password", required = true)
        public String password;

        //@Parameter(names = {"-p"}, description = "RabbitMQ port")
        public int port;

        public Config(String[] argv)
        {
            this.hostIP = argv[0];
            this.username = argv[1];
            this.password = argv[2];
            this.port = Convert.ToInt32(argv[3]);
        }
        /**
     * Creates a config object using the CLI arguments
     *
     * @param argv CLI arguments
     * @return util.Config instance instantiated with CLI arguments
     */
        public static Config readConfigFromCLIArgs(String[] argv) {

            Config config = new Config(argv);

            return config;
        }
    }
}