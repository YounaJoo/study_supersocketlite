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

        private List<Omok> OmokList = new List<Omok>();
        
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
            
            
            pakcetHandlerMap.Add((int)PACKETID.REQ_OMOK_GAME, requestOmokGame);
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

        public int GetUsableRoom()
        {
            int index = -1;
            foreach (var room in RoomList)
            {
                if (room.CurrentUserCount() != 1)
                {
                    continue;
                }
                index = room.Index;
                break;
            }

            if (index != -1)
            {
                return index;
            }

            foreach (var room in RoomList)
            {
                if (room.ChkRoomFull())
                {
                    continue;
                }
                index = room.Index;
                break;
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
                
                int roomNumber = GetUsableRoom();
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
                room.NotifyPacketUserList(sessionID); // Room에 있는 유저 정보
                room.NofifyPacketNewUser(sessionIndex, user.ID()); // 새로운 유저 정보
                
                ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID, user.UserPos); // 유저 정보
                
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
            
            MainServer.MainLogger.Info("RoomEnterResponse");
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
        
        public void requestOmokGame(ServerPacketData packetData)
        {
            var sessionIndex = packetData.SessionIndex;
            var sessionID = packetData.SessionID;

            var user = UserMgr.GetUser(sessionIndex);
            var roomObject = CheckRoomAndRoomUser(sessionIndex);
            
            if (user == null || roomObject.Item1 == false) // 유저가 없을 경우 에러
            {
                responseOmokGameToClinet(ERROR_CODE.OMOK_GAME_INVALIED_PACKET, sessionID);
                return;
            }

            var reqData = MessagePackSerializer.Deserialize<OMKReqOmokGame>(packetData.BodyData);
            var reqOmok = new Omok(reqData.X, reqData.Y);
            
            // min과 max 를 넘어서면 error
            if (reqOmok.x < OmokManager.MIN_X || reqOmok.x > OmokManager.MAX_X ||
                reqOmok.y < OmokManager.MIN_Y || reqOmok.y > OmokManager.MAX_Y)
            {
                responseOmokGameToClinet(ERROR_CODE.OMOK_GAME_INVALIED_POSITION, sessionID);
                return;
            }

            // 해당 패킷에 있는 x와 y에서 가장 가까운 점을 찾고(근사값) isActivity 체크
            float tempX = (float)Math.Round(reqOmok.x - (reqOmok.x % OmokManager.DIS), 2);
            float tempY = (float)Math.Round(reqOmok.y - (reqOmok.y % OmokManager.DIS), 2);
            
            var newOmok = new Omok(tempX, tempY);
            
            // Omok 있는지 찾기
            if (OmokList.IsEmpty()) // 
            {
                newOmok.setActivity(true);
                OmokList.Add(newOmok);
            }
            else
            {
                foreach (var omok in OmokList)
                {
                    if (omok.x == newOmok.x && omok.y == newOmok.y && omok.isActivity)
                    {
                        responseOmokGameToClinet(ERROR_CODE.OMOK_GAME_ALREADY_OMOK, sessionID);
                        return;
                    }
                }
                newOmok.setActivity(true);
                OmokList.Add(newOmok);
            }
            
            MainServer.MainLogger.Info($"reqOmok X : {reqOmok.x} Y : {reqOmok.y}");
            MainServer.MainLogger.Info($"NewOmok X : {newOmok.x} Y : {newOmok.y}");
            // Notify
            
            roomObject.Item2.NotifyPacketOmokGame(newOmok.x, newOmok.y, user.UserPos);
            responseOmokGameToClinet(ERROR_CODE.NONE, sessionID);
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

        void ResponseGameReadyToClient(ERROR_CODE errorCode, string sessionID)
        {
            var resRoomEnter = new OMKResGameReady()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.RES_GAME_READY, bodyData);
            
            MainServer.MainLogger.Info("RESGAMEREADY");
            // Room 에 잘 들어갔다고 MainServer.SendData 실행
            ServerNetwork.SendData(sessionID, sendData);
        }
        
        public void responseOmokGameToClinet(ERROR_CODE errorCode, string sessionID)
        {
            var resOmokGame = new OMKResOmokGame()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resOmokGame);
            var sendData = PacketToBytes.Make(PACKETID.RES_OMOK_GAME, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
            
            MainServer.MainLogger.Info($"RequestOmok, Send Response Omok Message : {resOmokGame.Result}");
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
                if (user == null)
                {
                    ResponseGameReadyToClient(ERROR_CODE.GAME_READY_INVALIED_USER, sessionID);
                    return; // error
                }
                
                var reqData = MessagePackSerializer.Deserialize<OMKReqGameReady>(packetData.BodyData);
                short userPos = reqData.UserPos;

                if (userPos != user.UserPos || userPos == -1)
                {
                    ResponseGameReadyToClient(ERROR_CODE.GAME_READY_INVALID_STATE, sessionID);
                    return;
                }

                var room = GetRoom(user.RoomNumber);
                // Player 2의 게임 준비 결과 반환
                if (userPos == 1)
                { 
                    room.isReady[user.UserPos] = true;
                    ResponseGameReadyToClient(ERROR_CODE.NONE, sessionID);
                    return;
                }
                
                // Player 1일 때 다른 플레이어가 아직 준비가 되어있지 않으면 오류
                if (room.isReady[1] == false)
                {
                    // error 반환
                    ResponseGameReadyToClient(ERROR_CODE.GAME_READY_INVALID_CHECK_OTHER_USER, sessionID);
                    return;
                }
                
                // Player 1의 결과를 true 로 변환하고, 모든 유저들에게 게임 시작한다는 알림
                room.isReady[user.UserPos] = true;
                // 반복되는 부분인가? 사용하지 않아도 되는가?
                foreach (var isReady in room.isReady)
                {
                    if (isReady == false)
                    {
                        ResponseGameReadyToClient(ERROR_CODE.GAME_READY_INVALID_STATE, sessionID);
                        return;
                    }
                }
                
                // 결과 반환 Noti -> Response -> GameStart
                room.NofifyPacketGameReady();
            } 
            
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }
    }
}