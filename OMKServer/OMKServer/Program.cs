using System;
using System.Threading;

namespace OMKServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverOption = ParseCommandLine(args);
            if (serverOption == null)
            {
                return;
            }
            
            var serverApp = new MainServer();
            serverApp.InitConfig(serverOption);
            
            serverApp.CreateStartServer();
            MainServer.MainLogger.Info("Press q to shut down the server");

            while (true)
            {
                Thread.Sleep(50);

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.KeyChar == 'q')
                    {
                        Console.WriteLine("Server Terminate~~");
                        serverApp.StopServer();
                        break;
                    } 
                }
            }
        }
        
        static OMKServerOption ParseCommandLine(string[] args)
        {
            var result =
                CommandLine.Parser.Default.ParseArguments<OMKServerOption>(args) as
                    CommandLine.Parsed<OMKServerOption>;

            if (result == null)
            {
                Console.WriteLine("Failed Command Line");
                return null;
            }

            return result.Value;
        }
    }
}