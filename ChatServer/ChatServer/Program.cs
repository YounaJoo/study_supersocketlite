using System;
using System.Threading;

namespace ChatServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // CommandLine에 입력한 데이터대로 serverOption 매핑
            var serverOption = ParseCommandLine(args);
            // 입력한 데이터가 없거나 매핑에 실패했으면 return
            if (serverOption == null)
            {
                return;
            }
            
            // AppServer를 상속받는 MainServer 객체 선언
            var serverApp = new MainServer();
            // 네트워크 설정값 초기화
            serverApp.InitConfig(serverOption);
            
            // 네트워크 생성 및 시작
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

        static ChatServerOption ParseCommandLine(string[] args)
        {
            var result =
                CommandLine.Parser.Default.ParseArguments<ChatServerOption>(args) as
                    CommandLine.Parsed<ChatServerOption>;

            if (result == null)
            {
                Console.WriteLine("Failed Command Line");
                return null;
            }

            return result.Value;
        }
    }
}