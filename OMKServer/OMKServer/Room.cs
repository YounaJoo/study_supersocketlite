using CSBaseLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OMKServer
{
    public class Room
    {
        public int Index { get; private set; }
        public int Number { get; private set; }

        private int MaxUserCount = 0;

        public List<Omok> OmokList = new List<Omok>();
        List<RoomUser> UserList = new List<RoomUser>();
        

        // index --> userPos 
        public bool[] isReady { get; set; } = new[] {false, false}; 
        
        // Func 반환값이 있는 메소드를 참조하는 델리게이트 변수
        public static Func<string, byte[], bool> NetSendFunc;

        public void Init(int index, int number, int maxUserCount)
        {
            this.Index = index;
            this.Number = number;
            this.MaxUserCount = maxUserCount;
        }

        public bool ChkRoomFull() // 꽉 차있으면 return --> 그냥 넘어가게
        {
            if (UserList.Count >= MaxUserCount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        // 요청을 보낸 유저를 룸에 들어와있는 형식으로 '추가'
        public bool AddUser(string userID, int netSessionIndex, string netSessionID)
        {
            if (GetUser(userID) != null)
            {
                return false;
            }

            var roomUser = new RoomUser();
            roomUser.Set(userID, netSessionIndex, netSessionID);
            UserList.Add(roomUser);

            return true;
        }

        public void RemoveUser(int netSessionIndex)
        {
            var index = UserList.FindIndex(x => x.NetSessionIndex == netSessionIndex);
            UserList.RemoveAt(index);
        }

        public bool RemoveUser(RoomUser user)
        {
            // 매개변수로 전달된 user 를 찾아 삭제 후 삭제 결과 반환
            return UserList.Remove(user);
        }

        public RoomUser GetUser(string userID)
        {
            return UserList.Find(x => x.UserID == userID);
        }

        public RoomUser GetUser(int netSessionIndex)
        {
            return UserList.Find(x => x.NetSessionIndex == netSessionIndex);
        }

        public int CurrentUserCount()
        {
            return UserList.Count();
        }

        // Client에게 보낼 데이터 
        public void NotifyPacketUserList(string userNetSessionID)
        {
            // Message Packet 
            var packet = new CSBaseLib.OMKRoomUserList();
            foreach (var user in UserList)
            {
                int i = UserList.IndexOf(user);
                packet.UserIDList[i] = user.UserID;
            }

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_USER_LIST, bodyData);

            // 델리게이트 실행 --> MainServer.SendData
            NetSendFunc(userNetSessionID, sendPacket);
        }

        public void NofifyPacketNewUser(int newUserNetSessionIndex, string newUserID)
        {
            var packet = new OMKRoomNewUser();
            packet.UserID = newUserID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_NEW_USER, bodyData);
            
            Broadcast(newUserNetSessionIndex, sendPacket);
        }

        // 유저가 방 나가는 경우
        public void NofifyPacketLeaveUser(string userID)
        {
            if (CurrentUserCount() == 0)
            {
                return;
            }
            
            var packet = new OMKRoomLeaveUser();
            packet.UserID = userID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPakcet = PacketToBytes.Make(PACKETID.NTF_ROOM_LEAVE_USER, bodyData);
            
            Broadcast(-1, sendPakcet);
        }
        
        public void NofifyPacketGameReady()
        {
            var resRoomEnter = new OMKResGameReady()
            {
                Result = (short)ERROR_CODE.NONE
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.RES_GAME_READY, bodyData);
            
            Broadcast(-1, sendData);
        }

        public void NofifyPacketGameStart()
        {
            // 이게 보내지면 GameStart
            var packet = new OMKNtfGameReady()
            {
                Result = (short) ERROR_CODE.NONE
            };

            var body = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_GAME_READY, body);
            
            Broadcast(-1, sendPacket);
        }

        public void NotifyPacketOmokGame(float x, float y, short userPos)
        {
            var packet = new OMKNtfOmokGame()
            {
                X = x,
                Y = y,
                UserPos = userPos
            };

            var body = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_OMOK_GAME, body);
            
            MainServer.MainLogger.Info("NotifyPacketOmokGame");
            
            Broadcast(-1, sendPacket);
        }
        
        public void Broadcast(int excludeNetSessionIndex, byte[] sendPacket)
        {
            foreach (var user in UserList)
            {
                if (user.NetSessionIndex == excludeNetSessionIndex)
                {
                    continue;
                }

                Console.WriteLine($"BroadCast : {user.NetSessionIndex}");
                NetSendFunc(user.NetSessionID, sendPacket);
            }
        }
    }
}