using CSBaseLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public int GetUserCount()
        {
            return UserList.Count;
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
        // 유저를 불러오지 못하면 FAIL
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

        // Client에게 보낼 데이터 
        public void NotifyPacketUserList(string userNetSessionID)
        {
            // Message Packet 
            var packet = new CSBaseLib.OMKRoomUserList();
            foreach (var user in UserList)
            {
                packet.UserIDList.Add(user.UserID);
            }

            // MessagePack, 데이터 직렬화
            var bodyData = MessagePackSerializer.Serialize(packet);
            // client에게 보낼 Packet을 하나로 뭉치는 작업하기
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_USER_LIST, bodyData);

            // 델리게이트 실행 --> MainServer.SendData
            NetSendFunc(userNetSessionID, sendPacket);
        }

        // Room에 새로운 유저 추가했다고 널리 알림
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
            
            // packet 속 해당 userID는 삭제되었다고 알려주려고 함.
            // 이 방에 저 userID는 나갔으니까 모두 다 알라~~함
            var packet = new OMKRoomLeaveUser();
            packet.UserID = userID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPakcet = PacketToBytes.Make(PACKETID.NTF_ROOM_LEAVE_USER, bodyData);
            
            // 자기 자신을 포함하여 룸에 있었던 모든 유저에게 SendData
            Broadcast(-1, sendPakcet);
        }
        
        // 해당 명령을 수행했다고 널리널리 알림
        public void Broadcast(int excludeNetSessionIndex, byte[] sendPacket)
        {
            // broadCast에서 사용하는 UserList는 RoomUser 객체를 저장하고 있는 List 이다.
            // 즉 해당되는 Room객체에 접속되어 있는 User들을 탐색하는 부분이다
            foreach (var user in UserList)
            {
                //Console.WriteLine($"BroadCast : {user.NetSessionIndex}");
                // 방 입장 && 방 나가기 SessionIndex 확인 
                if (user.NetSessionIndex == excludeNetSessionIndex)
                {
                    continue;
                }

                // chat 시, 혹은 여러 명이 존재하는 Room 입장 시 
                // 방 입장 시, 자기자신은 continue, 자기자신이 아닐 경우 SendData를 보낸다.
                // Q> 이 보내는 데이터는 누가 받는거지? --> 요청을 보낸 Client? 유저가 새롭게 들어온다는 소식을 받아야 하는 다른 Client? 
                // Q> 다른 유저들은 어찌 알게???
                // A> BroadCast 를 통해서!! 다른 Session에 SendData를 진행한다.
                Console.WriteLine($"BroadCast : {user.NetSessionIndex}");
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