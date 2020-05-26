using System;
using System.Collections.Generic;

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
        // Chatting Server의 CommandOption 객체
        public static ChatServerOption ServerOption;
        public static SuperSocket.SocketBase.Logging.ILog MainLogger;

        // ServerConfig Interface
        private SuperSocket.SocketBase.Config.IServerConfig m_Config;
        
        PacketProcessor MainPacektProcessor = new PacketProcessor();
        RoomManager RoomMgr = new RoomManager();

        // MainServer 생성자 선언 및 콜백 함수 연결
        public MainServer() : base(new DefaultReceiveFilterFactory<ReceiveFilter, EFBinaryRequestInfo>())
        {
            NewSessionConnected += new SessionHandler<ClientSession>(OnConnected);
            SessionClosed += new SessionHandler<ClientSession, CloseReason>(OnClosed);
            NewRequestReceived += new RequestHandler<ClientSession, EFBinaryRequestInfo>(OnPacketReceived);
        }

        // 네트워크 설정(Configuration) 초기화
        public void InitConfig(ChatServerOption option)
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

        public void CreateStartServer()
        {
            try
            {
                bool bResult = Setup(new RootConfig(), m_Config, logFactory: new NLogLogFactory());

                if (bResult == false)
                {
                    Console.WriteLine("[Error] 서버 네트워크 설정 실패 ㅠㅠ");
                    return;
                }
                else
                {
                    MainLogger = base.Logger;
                    MainLogger.Info("서버 초기화 성공");
                }

                CreateComponent();
                // SuperSocket Start --> AsyncAccept 까지
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

            //MainPacektProcessor.Destroy();
        }

        public ERROR_CODE CreateComponent()
        {

            return ERROR_CODE.NONE;
        }

        void OnConnected(ClientSession session)
        {
             
        }

        void OnClosed(ClientSession session, CloseReason reason)
        {
            
        }

        void OnPacketReceived(ClientSession session, EFBinaryRequestInfo reqInfo)
        {
            
        }
    }
}