using System;
using System.Collections.Generic;
using CSBaseLib;
using MessagePack;

namespace ChatServer
{
    public class PKHCommon : PKHandler
    {
        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> pakcetHandlerMap)
        {
            pakcetHandlerMap.Add((int)PACKETID.NTF_IN_CONNECT_CLIENT, NotifyInConnectClient);
            pakcetHandlerMap.Add((int)PACKETID.NTF_IN_DISCONNECT_CLIENT, NotifyInDisConnectClient);
            
            pakcetHandlerMap.Add((int)PACKETID.REQ_LOGIN, RequestLogin);
        }

        public void NotifyInConnectClient(ServerPacketData requestData)
        {
            MainServer.MainLogger.Debug($"Current Connected Session Count : {ServerNetwork.SessionCount}");
        }

        public void NotifyInDisConnectClient(ServerPacketData requestData)
        {
            var sessionIndex = requestData.SessionIndex;
            var user = UserMgr.GetUser(sessionIndex);

            if (user != null)
            {
                var roomNum = user.RoomNumber;

                // 현재 유저가 방에 들어간 채 접속 끊기 버튼을 눌렀다면 room에 들어가있는 user 정보 삭제
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
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("로그인 요청 받음");

            try
            {
                // 해당 sessionIndex를 가지고 있는 User 객체가 return 되면
                if (UserMgr.GetUser(sessionIndex) != null)
                {
                    // Error Code 를 보내고 return
                    ResponseLoginToClient(ERROR_CODE.LOGIN_ALREADY_WORKING, packetData.SessionID);
                    return;
                }

                // MessagePack : 메시지 직렬화
                var reqData = MessagePackSerializer.Deserialize<PKTReqLogin>(packetData.BodyData);
                var errorCode = UserMgr.AddUser(reqData.UserID, packetData.SessionID, packetData.SessionIndex);
                if (errorCode != ERROR_CODE.NONE)
                {
                    ResponseLoginToClient(errorCode, packetData.SessionID);

                    if (errorCode == ERROR_CODE.LOGIN_FULL_USER_COUNT)
                    {
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
            var resLogin = new PKTResLogin()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resLogin);
            var sendData = PacketToBytes.Make(PACKETID.RES_LOGIN, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }

        public void NotifyMustCloseToClient(ERROR_CODE errorCode, string sessionID)
        {
            var resLogin = new PKNtfMustClose()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resLogin);
            var sendData = PacketToBytes.Make(PACKETID.NTF_MUST_CLOSE, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }
    }
}