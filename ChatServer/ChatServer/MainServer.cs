using System;
using System.Collections.Generic;
using System.Threading;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketEngine;

using CSBaseLib;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Logging;

namespace ChatServer
{
    public class MainServer : AppServer<ClientSession, EFBinaryRequestInfo>
    {
        // Chatting Server의 CommandOption 객체와 MainLogger --> static
        public static ChatServerOption ServerOption;
        public static SuperSocket.SocketBase.Logging.ILog MainLogger;

        // ServerConfig Interface 
        private SuperSocket.SocketBase.Config.IServerConfig m_Config;
        
        PacketProcessor MainPacektProcessor = new PacketProcessor();
        RoomManager RoomMgr = new RoomManager();

        // MainServer 생성자 선언 및 이벤트 연결
        public MainServer() : base(new DefaultReceiveFilterFactory<ReceiveFilter, EFBinaryRequestInfo>())
        {
            NewSessionConnected += new SessionHandler<ClientSession>(OnConnected); // 1. 접속 연결
            SessionClosed += new SessionHandler<ClientSession, CloseReason>(OnClosed); // 2. 접속 해제
            NewRequestReceived += new RequestHandler<ClientSession, EFBinaryRequestInfo>(OnPacketReceived); // 3. 송신
        }

        // 네트워크 설정(Configuration) 초기화
        public void InitConfig(ChatServerOption option)
        {
            // 초기화 시, 매개변수로 넘겨받은 네트워크 설정을 담고있는 객체 option(ChatServerOption)을
            // MainServer의 static 으로 올려져 있는 ServerOption에 복사 --> 프로세스 진행하면서 해당 객체를 호출하며 재사용
            ServerOption = option;
            
            // ServerConfig 인터페이스에 ServerConfig 객체 인스턴스
            m_Config = new ServerConfig
            {
                // Name, Ip, Port, Mode, MaxConnectionNumber, 등등 필요한 데이터 프러퍼티 Set
                Name = option.Name,
                Ip = "Any",
                Port = option.Port,
                Mode = SocketMode.Tcp,
                MaxConnectionNumber = option.MaxConnectionNumber,
                MaxRequestLength = option.MaxRequestLength,
                ReceiveBufferSize = option.ReceiveBufferSize,
                SendBufferSize = option.SendBufferSize
            };
        }

        // 네트워크 설정 및 서버 생성
        public void CreateStartServer()
        {
            try
            {
                // RootConfig, ServerOption(m_config), logFactory 를 인자값으로 네트워크 셋팅
                // logFactory: 명명된 인수, Setup함수가 가지고 있는 매개변수 중 logFactory를 무엇으로 지정해주는 것 
                bool bResult = Setup(new RootConfig(), m_Config, logFactory: new NLogLogFactory());

                if (bResult == false)
                {
                    Console.WriteLine("[Error] 서버 네트워크 설정 실패");
                    return;
                }
                else
                {
                    // Main Logger 설정
                    MainLogger = base.Logger;
                    MainLogger.Info("서버 초기화 성공");
                }

                CreateComponent();
                // SuperSocket Start --> AsyncAccept 까지, 
                // 요청이 완료되면, 서버 생성 성공
                Start();
                MainLogger.Info("서버 생성 성공");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Error] 서버 생성 실패 : {e.ToString()}");
            }
        }

        public void StopServer()
        {
            // Super Socket Stop
            Stop();

            MainPacektProcessor.Destroy();
        }

        // 네트워크 요소 생성
        public ERROR_CODE CreateComponent()
        {
            // Session Index Pool 생성 - CommandLine에서 입력한 MaxConnectionNumber를 한정으로!
            // Q> MaxConnectionNumber 수를 넘어 선 채 세션을 할당한다면 어떻게 될 것인가?
            // Q> Index Pool의 역할은? 
            // A> 새로운 Client 유저 접속 시, Pool에 있는 index를 부여해주며 메모리 재사용 관리
            ClientSession.CreateIndexPool(m_Config.MaxConnectionNumber);

            // Room의 NetSendFunc 델리게이트 함수 연결
            // SendData : 처리를 끝내고 결과를 Client에게 Send 해주는 함수
            Room.NetSendFunc = this.SendData;
            // 채팅 방에 대한 설정값 만들기 (방 개수 Max, 한 방에 들어가는 유저 수, 방 번호 등) 
            RoomMgr.CreateRooms();
            
            // 패킷 프로세서 인스턴스
            MainPacektProcessor = new PacketProcessor();
            MainPacektProcessor.CreateAndStart(RoomMgr.GetRoomsList(), this);
            
            MainLogger.Info("CreateComponent - Success");
            
            return ERROR_CODE.NONE;
        }
        
        // PKHCommon 개발하다가 넘어옴
        // 처리를 끝내고 결과를 Client에게 Send 해주는 함수
        public bool SendData(string sessionID, byte[] sendData)
        {
            // GetSessionByID : AppServer 기능
            // Q> 궁금하니까 session을 출력해보도록 하자.. 어떤 값을 받아오는가?? 
            // A> Client의 Session 정보를 가지고 있다. (sessionIndex 등) (TAppSession Type이라서 session은 ClientSession)
            var session = GetSessionByID(sessionID);

            try
            {
                if (session == null)
                {
                    return false;
                }
                Console.WriteLine($"SendData sesionIndex : {session.SessionIndex}");
                
                // 보내는 것도 SuperSocket이 해준다.
                session.Send(sendData, 0, sendData.Length);
            }
            catch (Exception e)
            {
                // TimeoutException 예외 발생할 수 있음
                MainServer.MainLogger.Error($"{e.ToString()}, {e.StackTrace}");
                
                session.SendEndWhenSendingTimeOut();
                session.Close();
            }

            return true;
        }
        
        // PKHCommon 개발하다가 넘어감
        // 1. MainServer 핸들러에 등록된 '접속, 접속 해제, 요청'에 대한 패킷정보를 담고 Processor에 Insert
        // (Receive 받은 대로 Processor Thread 에서 처리하는 기능) 
        public void Distribute(ServerPacketData requestPacket)
        {
            MainPacektProcessor.InsertPacket(requestPacket);
        }

        // '접속' 버튼 시
        void OnConnected(ClientSession session)
        {
            // 옵션의 최대 연결 수를 넘으면 SuperSocket이 바로 접속을 짤라버린다. 즉 이 OnConnected 함수는 호출되지 않는다.
            
            // Session Index Pool 에서 하나 Pop 하여 세션에 메모리 할당
            session.AllocSessionIndex();
            MainLogger.Info(string.Format("세션 번호 {0} 접속", session.SessionID));

            var packet =
                ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(true, session.SessionID,
                    session.SessionIndex);
            Distribute(packet);
        }

        // 접속 끊기
        void OnClosed(ClientSession session, CloseReason reason)
        {
            MainLogger.Info(string.Format("세션 번호 {0} 접속 해제 : {1}", session.SessionID, reason.ToString()));

            var packet =
                ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(false, session.SessionID,
                    session.SessionIndex);
            Distribute(packet);
            
            session.FreeSessionIndex(session.SessionIndex);
        }
        
        // '로그인', '방 입장', 'Chat', '방 나가기'
        void OnPacketReceived(ClientSession session, EFBinaryRequestInfo reqInfo)
        {
            MainLogger.Debug(string.Format("세션 번호 {0} 받은 데이터 크기 : {1} ThreadID : {2}", 
                session.SessionID, reqInfo.Body.Length, Thread.CurrentThread.ManagedThreadId));
            
            var packet = new ServerPacketData();
            packet.SessionID = session.SessionID;
            packet.SessionIndex = session.SessionIndex;
            packet.PackSize = reqInfo.Size;
            packet.PacketID = reqInfo.PacketID;
            packet.Type = reqInfo.Type;
            packet.BodyData = reqInfo.Body;

            Distribute(packet);
        }
    }
    class ConfigTemp
    {
        static public List<string> RemoteServers = new List<string>();
    }
}