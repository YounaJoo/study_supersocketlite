using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CSBaseLib;
using MessagePack;

namespace ChatServer
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
                if (room.GetUserCount() == 1)
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
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID);
                    return;
                }

                if (user.IsStateRoom())
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_STATE, sessionID);
                    return;
                }
                
                // Binary --> Object 직렬화
                var reqData = MessagePackSerializer.Deserialize<OMKReqRoomEnter>(packetData.BodyData);
                // room은 Room 객체
                
                // 06.04 Client는 항상 -1을 Request Body에 담아서 요청을 한다.
                // 방을 전부 탐색해서 만약 유저가 1명뿐인 방이 있으면 그 방으로 순차적으로 데이터를 넣어준다
                // 그렇게 넣어진 방을 return 한다.
                // 방 객체를 Buffer에 두고 Init 시, userCount 변수도 추가
                // 방 탐색하는 함수는 또 쓰일 수 있으니, 함수로 빼도록 하자
                int roomNumber = SelectRoom();
                var room = GetRoom(roomNumber);
                //var room = GetRoom(reqData.RoomNumber);
                
                Console.WriteLine("roomNumber : " + room.Number);
                
                //if (reqData.RoomNumber != -1 || room == null)
                if(room == null) // test 용
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER, sessionID);
                    return;
                }

                // UserList<RoomUser> 객체 추가
                if (room.AddUser(user.ID(), sessionIndex, sessionID) == false)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER, sessionID);
                    return;
                }
                
                // User 객체에 유저가 요청하고자 하는 방에 들어갔다고 설정
                user.EnteredRoom(roomNumber);
                // UserList 추가 됐다는 응답 : Packet 뭉침 --> MainServer.SendData
                room.NotifyPacketUserList(sessionID);
                // newUser 추가 됐다고 확인 : Packet 뭉침 --> BroadCast
                room.NofifyPacketNewUser(sessionIndex, user.ID());

                // Client가 문제 없이 Room에 입장했다고 응답 : Packet 뭉침 --> mainServer.SendData
                ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID);
                
                MainServer.MainLogger.Debug("RequestEnterInternal = Success");
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        void ResponseEnterRoomToClient(ERROR_CODE errorCode, string sessionID)
        {
            var resRoomEnter = new OMKResRoomEnter()
            {
                Result = (short)errorCode
            };

            // Object --> Binary (Class만 가능)
            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            //Console.WriteLine($"ResponseEnterRoomToClient BodyData : {Encoding.ASCII.GetString(bodyData)}");
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_ENTER, bodyData);
            //Console.WriteLine($"ResponseEnterRoomToClient sendData : {Encoding.ASCII.GetString(sendData)}");
            
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

            // 해당 룸의 객체를 불러옴
            var room = GetRoom(roomNumber);
            // 객체가 비어있으면 fail
            if (room == null)
            {
                return false;
            }

            // sessionIndex를 사용해서 해당 룸의 RoomUser를 Get
            var roomUser = room.GetUser(sessionIndex);
            if (roomUser == null)
            {
                return false;
            }

            // sessionIndex-room에 있는 userID를 get 
            var userID = roomUser.UserID;
            // 해당 userID를 roomUser에서 삭제
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

        
        // Client는 접속이 끊는다 하였는데 룸에는 입장되어 있는 상황일떄
        // 이미 이전에 접속 되어있는지 아닌지는 체크를 했다. 이 부분은 체크를 했으니 룸 객체에서 유저를 빼내는 부분을 해야 할 것이다
        // 예상 프로세스 : ServerPacketData Object에서 SessionIndex Get -> sessionIndex로 User 객체 Get
        // --> 현 User 객체가 들어가 있는 roomNumber, Room Get --> 해당 Room 객체에서 RoomUser.Remove 
        // 결과 : sessionIndex로 User 객체 Get
        // --> roomNumber를 이용해 현 User 객체가 들어가 있는 Room Get --> 해당 Room 객체에서 RoomUser Get
        // RoomUser에 속해있는 객체의 데이터 중, 해당 userID를 Get --> RoomUser에서 Remove
        // --> 자기자신을 포함한 모든 RoomUser에게 이 userID는 방을 나갔다고 알림.
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
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("Room RequestChat");

            try
            {
                // 반환 결과, room, roomUser Object 
                var roomObject = CheckRoomAndRoomUser(sessionIndex);

                // 만약 객체 반환에 실패했다면 return --> MainLogger에는 Request 만 받았다고 출력됨
                if (roomObject.Item1 == false)
                {
                    return;
                }

                // Packet의 Body Data에 대한 메세지 직렬화
                var reqData = MessagePackSerializer.Deserialize<OMKReqRoomChat>(packetData.BodyData);
                
                // MessagePack
                var notifyPacket = new OMKRoomChat()
                {
                    // RoomUser의 userID, 여기에는 chat을 보내는 이의 ID
                    UserID = roomObject.Item3.UserID,
                    // body안에 있던 chatMessage
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
    }
}