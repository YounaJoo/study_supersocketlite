using CSBaseLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatServer
{
    // PakcetProcess 개발 하다가 넘어옴
    public class Room
    {
        public int Index { get; private set; }
        public int Number { get; private set; }

        private int MaxUserCount = 0;
        
        List<RoomUser> UserList = new List<RoomUser>();
        // Func 반환값이 있는 메소드를 참조하는 델리게이트 변수
        // 매개변수는 string, byte[]를 가짐
        public static Func<string, byte[], bool> NetSendFunc;

        public void Init(int index, int number, int maxUserCount)
        {
            this.Index = index;
            this.Number = number;
            this.MaxUserCount = maxUserCount;
        }

        // 
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
            // 해당 인덱스와 일치하는 해당 리스트 요소를 삭제
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

        // ??
        public void NotifyPacketUserList(string userNetSessionID)
        {
            // Message Packet 
            var packet = new CSBaseLib.PKTNtfRoomUserList();
            foreach (var user in UserList)
            {
                // MessagePacket List에 Room에 들어갈 UserID 추가 (?)
                packet.UserIDList.Add(user.UserID);
            }

            // MessagePack, 데이터 직렬화(object --> byte[])
            var bodyData = MessagePackSerializer.Serialize(packet);
            // Q> 직렬화된 bodyData와 PacketID를 가지고 Packet을 하나로 뭉침. --> 맞나 (?)
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_USER_LIST, bodyData);

            // 델리게이트 등록
            NetSendFunc(userNetSessionID, sendPacket);
        }

        // 새로운 유저 추가
        public void NofifyPacketNewUser(int newUserNetSessionIndex, string newUserID)
        {
            var packet = new PKTNtfRoomNewUser();
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
            
            var packet = new PKTNtfRoomLeaveUser();
            packet.UserID = userID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPakcet = PacketToBytes.Make(PACKETID.NTF_ROOM_LEAVE_USER, bodyData);
            
            Broadcast(-1, sendPakcet);
        }

        // Q> 어떤 프로세스로 흘러갈지 생각해보기.
        public void Broadcast(int excludeNetSessionIndex, byte[] sendPacket)
        {
            foreach (var user in UserList)
            {
                if (user.NetSessionIndex == excludeNetSessionIndex)
                {
                    continue;
                }

                NetSendFunc(user.NetSessionID, sendPacket);
            }
        }
    }

    public class RoomUser
    {
        public string UserID { get; private set; }
        public int NetSessionIndex { get; private set; }
        public string NetSessionID { get; private set; }

        public void Set(string userID, int netSessionIndex, string netSessionID)
        {
            this.UserID = userID;
            this.NetSessionIndex = netSessionIndex;
            this.NetSessionID = netSessionID;
        }
    }
}