using System;
using CommandLine;

namespace EchoServerEx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello SuperSocketLite");

            // 커맨드 라인에서 배치 파일 실행 후 Port, Max 연결 수, Name 입력 후 매핑
            var serverOption = ParseCommandLine(args);
            if (serverOption == null)
            {
                return;
            }
            
            // MainServer 객체 생성
            var server = new MainServer();
            // MainServer 객체 초기화
            server.InitConfig(serverOption);
            server.CreateServer();

            // Start = SuperSocket 기능, 연결 시작
            var IsResult = server.Start();

            if (IsResult)
            {
                MainServer.MainLogger.Info("서버 네트워크 시작");
            }
            else
            {
                Console.WriteLine("[Error] 서버 네트워크 시작 실패");
                return;
            }
            
            MainServer.MainLogger.Info("Key 를 누르면 종료");
            Console.ReadKey();
        }

        static ServerOption ParseCommandLine(string[] args)
        {
            var result =
                CommandLine.Parser.Default.ParseArguments<ServerOption>(args) as CommandLine.Parsed<ServerOption>;

            if (result == null)
            {
                Console.WriteLine("Failed Command Line");
                return null;
            }

            return result.Value;
        }
    }
    
    public class ServerOption
    {
        // CommandLine Option
        // 이름, 필수인지 아닌지, 설명 || 변수 명 프로퍼티
        // 실행 EchoServerEx.exe --port 10003(10,000이하는 쓰지 말기) --maxConnectionNumber 256 --name EchoServerEx
        [Option("port", Required = true, HelpText = "Server Port Number")]
        public int Port { get; set; }

        [Option("maxConnectionNumber", Required = true, HelpText = "MaxConnectionNumber Count")]
        public int MaxConnectionNumber { get; set; } = 0;
        
        [Option("name", Required = true, HelpText = "Server Name")]
        public string Name { get; set; }
    }
}