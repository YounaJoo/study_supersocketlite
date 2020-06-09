using System;
using System.Collections.Generic;
using CSBaseLib;
using MessagePack;

namespace OMKServer
{
    public class PKHCommon : PKHandler
    {
        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> pakcetHandlerMap)
        {
            pakcetHandlerMap.Add((int)PACKETID.NTF_IN_CONNECT_CLIENT, NotifyInConnectClient);
            pakcetHandlerMap.Add((int)PACKETID.NTF_IN_DISCONNECT_CLIENT, NotifyInDisConnectClient);
            
            pakcetHandlerMap.Add((int)PACKETID.REQ_LOGIN, RequestLogin);
        }

        // '접속' --> Accept 까지는 SuperSocket 역할, Server는 Log 저장
        public void NotifyInConnectClient(ServerPacketData requestData)
        {
            MainServer.MainLogger.Debug($"Current Connected Session Count : {ServerNetwork.SessionCount}");
        }

        // '접속 해제' 
        public void NotifyInDisConnectClient(ServerPacketData requestData)
        {
            var sessionIndex = requestData.SessionIndex;
            var user = UserMgr.GetUser(sessionIndex);

            if (user != null)
            {
                var roomNum = user.RoomNumber;

                // 현재 유저가 방에 들어간 채 접속 끊기 버튼을 눌렀다면 room에 들어가있는 user 정보 삭제
                // 꽤 많은 비용이 발생되지만 유지보수를 위해, 일반적으로 유저가 '방 나가기' 버튼을 눌렀을 때와 일관성 맞춤
                if (roomNum != PacketDef.INVALID_ROOM_NUMBER)
                {
                    var packet = new PKTInternalNTFRoomLeave()
                    {
                        RoomNumber = roomNum,
                        UserID = user.ID(),
                    };

                    // 데이터 직렬화
                    var pakcetBodyData = MessagePackSerializer.Serialize(packet);
                    var internalPacket = new ServerPacketData();
                    internalPacket.Assign("", sessionIndex, (Int16)PACKETID.NTF_IN_ROOM_LEAVE, pakcetBodyData);

                    // Receive Thread에 internalPacket 등록 --> 룸에서 유저만 삭제
                    ServerNetwork.Distribute(internalPacket);
                }

                // 접속 해제를 할 user 객체 삭제
                UserMgr.RemoveUser(sessionIndex);
            }
            
            MainServer.MainLogger.Debug($"Current Connected Session Count : {ServerNetwork.SessionCount}");
        }

        // Login 요청 받았을 시 PacketID와 연결되는 method
        public void RequestLogin(ServerPacketData packetData)
        {
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("로그인 요청 받음");

            try
            {
                // 로그인 중복 방지
                if (UserMgr.GetUser(sessionIndex) != null)
                {
                    ResponseLoginToClient(ERROR_CODE.LOGIN_ALREADY_WORKING, packetData.SessionID);
                    return;
                }
                
                var reqData = MessagePackSerializer.Deserialize<OMKReqLogin>(packetData.BodyData);
                var errorCode = UserMgr.AddUser(reqData.UserID, packetData.SessionID, packetData.SessionIndex);
                if (errorCode != ERROR_CODE.NONE)
                {
                    // 접속을 끊지 않아도 되는 에러
                    ResponseLoginToClient(errorCode, packetData.SessionID);
                    
                    if (errorCode == ERROR_CODE.LOGIN_FULL_USER_COUNT)
                    {
                        // 접속을 끊어야 하는 에러(클라이언트에서도 해도 되긴 하다.)
                        NotifyMustCloseToClient(ERROR_CODE.LOGIN_FULL_USER_COUNT, packetData.SessionID);
                    }

                    return;
                }

                ResponseLoginToClient(errorCode, packetData.SessionID);
                MainServer.MainLogger.Debug("로그인 요청 답변 보냄");
            }
            catch (Exception e)
            {
                // 패킷 해제에 의해서 로그가 남지 않도록 로그 수준을 Debug 한다.
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        public void ResponseLoginToClient(ERROR_CODE errorCode, string sessionID)
        {
            // new PacketMessage
            var resLogin = new OMKResLogin()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resLogin);
            var sendData = PacketToBytes.Make(PACKETID.RES_LOGIN, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }

        public void NotifyMustCloseToClient(ERROR_CODE errorCode, string sessionID)
        {
            var resLogin = new OMKMustClose()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resLogin);
            var sendData = PacketToBytes.Make(PACKETID.NTF_MUST_CLOSE, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }
    }
}