﻿using System;
using System.Collections.Generic;

using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Logging;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketBase.Config;

namespace EchoServerEx
{
    // NetworkSession-AppSession, EFBinaryRequestInfo-BinaryRequestInfo 를 매개변수로 받는 AppServer 상속받는 MainServer
    // AppServer는 SuperSocket의 Core 이다.
    public class MainServer : AppServer<NetworkSession, EFBinaryRequestInfo>
    {
        public static SuperSocket.SocketBase.Logging.ILog MainLogger;
        
        // Class를 Dictionary Value로 사용하는 HandlerMap 선언 
        Dictionary<int, Action<NetworkSession, EFBinaryRequestInfo>> HandlerMap = new Dictionary<int, Action<NetworkSession, EFBinaryRequestInfo>>();
        // 핸들러 객체 선언
        CommonHandler CommonHan = new CommonHandler();

        // SuperSocket.SocketBase.Config의 IServerConfig
        private IServerConfig m_config;

        // MainServer 생성자 
        public MainServer() : base(new DefaultReceiveFilterFactory<ReceiveFilter, EFBinaryRequestInfo>())
        {
            // Callback
            // 사용자 정의 Event 발생
            NewSessionConnected += new SessionHandler<NetworkSession>(OnConnected); // 1. 연결
            SessionClosed += new SessionHandler<NetworkSession, CloseReason>(OnClosed); // 2. 접속 끊김
            NewRequestReceived += new RequestHandler<NetworkSession, EFBinaryRequestInfo>(RequestReceived); // 3. 데이터 왔음
        }

        void RegistHandler()
        {
            // Map 이라 중복이 안됨. 핸들러의 정확한 개념을 인지하고 가기 --> createServer 호출 모양 잘 보기!
            HandlerMap.Add((int)PACKETID.REQ_ECHO, CommonHan.RequestEcho);
            MainLogger.Info("핸들러 등록 완료");
        }

        public void InitConfig(ServerOption option)
        {
            m_config = new ServerConfig
            {
                Port = option.Port,
                Ip = "Any",
                MaxConnectionNumber = option.MaxConnectionNumber,
                Mode = SocketMode.Tcp,
                Name = option.Name
            };
        }

        public void CreateServer()
        {
            try
            {
                // SuperSocket.SocketBase의 AppServer Method SetUp 호출
                // 인터페이스 IRootConfig를 상속받는 RootConfig, Server의 Config 정보를 담는 멤버 변수 m_config, NLog
                // 를 매개변수로 갖는 SetUp 함수를 호출하여 서버 네트워크 설정하고 결과값 매핑
                bool bResult = Setup(new RootConfig(), m_config, logFactory: new NLogLogFactory());

                if (bResult == false)
                {
                    Console.WriteLine("[Error] 서버 네트워크 설정 실패");
                    return;
                }
                else
                {
                    // MainServer의 MainLogger를 부모(AppServerBase)의 Logger를 참조하여 셋팅
                    MainLogger = base.Logger;
                }
                
                // 네트워크 설정에 성공 했으면 핸들러 등록 --> 서버 생성
                RegistHandler();
                MainLogger.Info("서버 생성 성공");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Error] 서버 생성 실행 : {e.ToString()}");
            }
        }

        // ?? 코드 진행 후 다시 정리해보기
        // ServerState 는 서버의 상태를 저장하는 열거형 데이터
        public bool IsRunning(ServerState eCurState)
        {
            if (eCurState == ServerState.Running)
            {
                return true;
            }

            return false;
        }

        void OnConnected(NetworkSession session)
        {
            // sessionId = NetWorkSession Class가 상속받고 있는 AppSession의 데이터 프로퍼티
            MainLogger.Info($"세션 번호 {session.SessionID} 접속");
        }

        void OnClosed(NetworkSession session, CloseReason reason)
        {
            MainLogger.Info($"세션 번호 {session.SessionID} 접속해제 : {reason.ToString()}");
        }

        // 요청을 받았을 때 핸들러에 의해 연결되는 콜백 함수
        void RequestReceived(NetworkSession session, EFBinaryRequestInfo reqInfo)
        {
            // 리퀘스트 정보에 들어있는 PacketID get
            var PacketID = reqInfo.PacketID;

             // 중복 키 발생 시
            if (HandlerMap.ContainsKey(PacketID))
            {
                // Q> 의미 무엇 ㅜㅜ?
                HandlerMap[PacketID](session, reqInfo);
            }
            else
            {
                // 무사히 요청을 받음
                MainLogger.Info($"세션 번호 {session.SessionID}, 받은 데이터 크기 : {reqInfo.Body.Length}");
                
            }
        }
    }

    // AppSession을 상속받는 NetworkSession 정의 필요
    public class NetworkSession : AppSession<NetworkSession, EFBinaryRequestInfo>
    {
        
    }
}