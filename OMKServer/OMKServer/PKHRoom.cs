using System;
using System.Collections.Generic;
using System.Linq;
using CSBaseLib;
using MessagePack;


namespace OMKServer
{
    public class PKHRoom : PKHandler
    {
        List<Room> RoomList = new List<Room>();
        private int StartRoomNumber;

        public void SetRoomList(List<Room> roomList)
        {
            RoomList = roomList;
            StartRoomNumber = roomList[0].Number;
        }

        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> pakcetHandlerMap)
        {
            pakcetHandlerMap.Add((int)PACKETID.REQ_ROOM_ENTER, RequestRoomEnter);
            pakcetHandlerMap.Add((int)PACKETID.REQ_ROOM_LEAVE, RequestLeave);
            pakcetHandlerMap.Add((int)PACKETID.NTF_IN_ROOM_LEAVE, NotifyLeaveInternal);
            pakcetHandlerMap.Add((int)PACKETID.REQ_ROOM_CHAT, RequestChat);
            
            pakcetHandlerMap.Add((int)PACKETID.REQ_GAME_READY, RequestGameReady);
        }

        Room GetRoom(int roomNumber)
        {
            var index = roomNumber - StartRoomNumber;

            // RoomList.Count 는 System.Linq Enumerable의 static Method
            if (index < 0 || index >= RoomList.Count())
            {
                return null;
            }

            return RoomList[index];
        }

        // tuple 사용
        (bool, Room, RoomUser) CheckRoomAndRoomUser(int userNetSessionIndex)
        {
            var user = UserMgr.GetUser(userNetSessionIndex);
            if (user == null)
            {
                return (false, null, null);
            }

            var roomNumber = user.RoomNumber;
            var room = GetRoom(roomNumber);

            if (room == null)
            {
                return (false, null, null);
            }

            var roomUser = room.GetUser(userNetSessionIndex);

            if (roomUser == null)
            {
                return (false, room, null);
            }

            return (true, room, roomUser);
        }

        public int SelectRoom()
        {
            int index = -1;
            foreach (var room in RoomList)
            {
                if (room.CurrentUserCount() == 1)
                {
                    index = room.Index;
                }
                else
                {
                    continue;
                }
            }

            if (index == -1)
            {
                foreach (var room in RoomList)
                {
                    if (room.ChkRoomFull() == false)
                    {
                        index = room.Index;
                        break;
                    }
                }
            }

            return index;
        }
        
        public void RequestRoomEnter(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("RequestRoonEnter");

            try
            {
                // user는 User 객체
                var user = UserMgr.GetUser(sessionIndex);

                if (user == null || user.IsConfirm(sessionID) == false)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID, -1);
                    return;
                }

                if (user.IsStateRoom())
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_STATE, sessionID, -1);
                    return;
                }
                
                var reqData = MessagePackSerializer.Deserialize<OMKReqRoomEnter>(packetData.BodyData);
                
                int roomNumber = SelectRoom();
                var room = GetRoom(roomNumber);
                
                if (reqData.RoomNumber != -1 || room == null)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER, sessionID, -1);
                    return;
                }

                // UserList<RoomUser> 객체 추가
                if (room.AddUser(user.ID(), sessionIndex, sessionID) == false)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER, sessionID, -1);
                    return;
                }

                if (room.CurrentUserCount() <= 1) { user.setUserPos(0);}
                else { user.setUserPos(1); }
                
                user.EnteredRoom(roomNumber);
                room.NotifyPacketUserList(sessionID);
                room.NofifyPacketNewUser(sessionIndex, user.ID());
                
                ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID, user.UserPos);
                
                MainServer.MainLogger.Debug("RequestEnterInternal = Success, UserPos : " + user.UserPos);
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }
        
        void ResponseEnterRoomToClient(ERROR_CODE errorCode, string sessionID, short userPos)
        {
            // 0609 여기에 userPos 
            // sessionID로 userPos
            var resRoomEnter = new OMKResRoomEnter()
            {
                Result = (short)errorCode,
                userPos = userPos
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_ENTER, bodyData);
            
            // Room 에 잘 들어갔다고 MainServer.SendData 실행
            ServerNetwork.SendData(sessionID, sendData);
        }

        public void RequestLeave(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("로그인 요청 받음");

            try
            {
                var user = UserMgr.GetUser(sessionIndex);
                if (user == null)
                {
                    return;
                }
                
                // 룸에서 유저 떠남
                if (LeaveRoomUser(sessionIndex, user.RoomNumber) == false)
                {
                    return;
                }
                
                // 유저 객체에 룸에서 떠났다고 알리기
                user.LeaveRoom();
                // 결과를 해당 유저에게 전달
                ResponseLeaveRoomToClient(sessionID);
                MainServer.MainLogger.Debug("Room RequestLeave - Success");
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }
        
        // 해당 룸에 (roomNumber) 해당 유저(sessinIndex) 떠남
        bool LeaveRoomUser(int sessionIndex, int roomNumber)
        {  
            MainServer.MainLogger.Debug($"LeaveRoomUser. SessionIndex:{sessionIndex}");
            
            var room = GetRoom(roomNumber);
            if (room == null)
            {
                return false;
            }

            var roomUser = room.GetUser(sessionIndex);
            if (roomUser == null)
            {
                return false;
            }

            var userID = roomUser.UserID;
            room.RemoveUser(roomUser);
            
            // Pakcet을 뭉쳐서 모든 유저에게 SendData
            room.NofifyPacketLeaveUser(userID);
            return true;
        }

        void ResponseLeaveRoomToClient(string sessionID)
        {
            // MessagePack
            var resRoomLeave = new OMKResRoomLeave()
            {
                Result = (short)ERROR_CODE.NONE
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomLeave);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_LEAVE, bodyData);

            // 해당 유저에게 SendData
            ServerNetwork.SendData(sessionID, sendData);
        }
        
        public void NotifyLeaveInternal(ServerPacketData packetData)
        {
            // ServerPacketData Object에서 SessionIndex Get
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug($"NotifyLeaveInternal. SessionIndex:{sessionIndex}");

            // MessagePack 직렬화 (RoomNumber, UserID)
            var reqData = MessagePackSerializer.Deserialize<PKTInternalNTFRoomLeave>(packetData.BodyData);
            LeaveRoomUser(sessionIndex, reqData.RoomNumber);
        }

        public void RequestChat(ServerPacketData packetData)
        {
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("Room RequestChat");

            try
            {
                var roomObject = CheckRoomAndRoomUser(sessionIndex);

                if (roomObject.Item1 == false)
                {
                    return;
                }

                var reqData = MessagePackSerializer.Deserialize<OMKReqRoomChat>(packetData.BodyData);
                
                var notifyPacket = new OMKRoomChat()
                {
                    UserID = roomObject.Item3.UserID,
                    ChatMessage = reqData.ChatMessage
                };

                // Packet 직렬화
                var body = MessagePackSerializer.Serialize(notifyPacket);
                var sendData = PacketToBytes.Make(PACKETID.NTF_ROOM_CHAT, body);
                
                // BroadCast -1 이니까 모든 Room에 있는 user에게! 
                roomObject.Item2.Broadcast(-1, sendData);
                
                MainServer.MainLogger.Debug("Room RequestChat - Success");
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        public void RequestGameReady(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("게임 준비 요청 받음");
            
            try
            {
                // User 객체
                var user = UserMgr.GetUser(sessionIndex);
                
            }
            
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }
    }
}