using System;
using System.Threading;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

using CSBaseLib;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Logging;

namespace OMKServer
{
   public class MainServer : AppServer<ClientSession, OMKBinaryRequestInfo>
    {
        public static OMKServerOption ServerOption;
        public static SuperSocket.SocketBase.Logging.ILog MainLogger;

        private SuperSocket.SocketBase.Config.IServerConfig m_Config;
        
        PacketProcessor MainPacektProcessor = new PacketProcessor();
        RoomManager RoomMgr = new RoomManager();

        // MainServer 생성자 선언 및 이벤트 연결
        public MainServer() : base(new DefaultReceiveFilterFactory<ReceiveFilter, OMKBinaryRequestInfo>())
        {
            NewSessionConnected += new SessionHandler<ClientSession>(OnConnected); // 1. 접속 연결
            SessionClosed += new SessionHandler<ClientSession, CloseReason>(OnClosed); // 2. 접속 해제
            NewRequestReceived += new RequestHandler<ClientSession, OMKBinaryRequestInfo>(OnPacketReceived); // 3. 송신
        }

        // 네트워크 설정(Configuration) 초기화
        public void InitConfig(OMKServerOption option)
        {
            ServerOption = option;
            
            m_Config = new ServerConfig
            {
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
                bool bResult = Setup(new RootConfig(), m_Config, logFactory: new NLogLogFactory());

                if (bResult == false)
                {
                    MainLogger.Error("[Error] 서버 네트워크 설정 실패");
                    return;
                }
                else
                {
                    // Main Logger 설정
                    MainLogger = base.Logger;
                    MainLogger.Info("서버 초기화 성공");
                }

                CreateComponent();
                // SuperSocket Start --> AsyncAccept
                Start();
                MainLogger.Info("서버 생성 성공");
            }
            catch (Exception e)
            {
                MainLogger.Error($"[Error] 서버 생성 실패 : {e.ToString()}");
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
            ClientSession.CreateIndexPool(m_Config.MaxConnectionNumber);

            // Room의 NetSendFunc 델리게이트 함수 연결 
            // Q> 델리게이트 함수를 쓴 이유 -> 유지보수 상 MainServer를 instance를 할당하거나 static을 하지 않더라도 타 Class에서 사용 용이
            Room.NetSendFunc = this.SendData;
            RoomMgr.CreateRooms();
            
            // 패킷 프로세서 인스턴스
            MainPacektProcessor = new PacketProcessor();
            MainPacektProcessor.CreateAndStart(RoomMgr.GetRoomsList(), this);
            
            MainLogger.Info("CreateComponent - Success");
            
            return ERROR_CODE.NONE;
        }
        
        public bool SendData(string sessionID, byte[] sendData)
        {
            var session = GetSessionByID(sessionID);

            try
            {
                if (session == null)
                {
                    return false;
                }
                
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
        
        // Receive 받은 대로 Processor Thread 에서 처리
        public void Distribute(ServerPacketData requestPacket)
        {
            MainPacektProcessor.InsertPacket(requestPacket);
        }

        // '접속' 
        void OnConnected(ClientSession session)
        {
            session.AllocSessionIndex();
            MainLogger.Info(string.Format("세션 번호 {0} 접속", session.SessionID));

            var packet = ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(true, session.SessionID, session.SessionIndex);
            MainLogger.Info("접속 시 세션 번호 : " + session.SessionIndex);
            Distribute(packet);
        }

        // '접속 끊기'
        void OnClosed(ClientSession session, CloseReason reason)
        {
            MainLogger.Info(string.Format("세션 번호 {0} 접속 해제 : {1}", session.SessionID, reason.ToString()));

            var packet =
                ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(false, session.SessionID,
                    session.SessionIndex);
            Distribute(packet);
            
            session.FreeSessionIndex(session.SessionIndex);
        }
        
        // 'User Action'
        void OnPacketReceived(ClientSession session, OMKBinaryRequestInfo reqInfo)
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
}