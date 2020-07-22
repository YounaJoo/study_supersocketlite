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
        
        List<RoomUser> UserList = new List<RoomUser>();
        
        private int[,] omok = new int[OmokManager.OMOK_COUNT + 1,OmokManager.OMOK_COUNT + 1];

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

        public void initOmok()
        {
            for (int y = 0; y < OmokManager.OMOK_COUNT; y++)
            {
                for (int x = 0; x < OmokManager.OMOK_COUNT; x++)
                {
                    omok[y, x] = -1;
                }
            }
        }
        
        public void initReady()
        {
            for (int i = 0; i < isReady.Length; i++)
            {
                isReady[i] = false;
            }
        }
        
        // omok 비어있는지 체크하고 userPos 입력
        public ERROR_CODE ChkOmokPosition(int x, int y, short userPos)
        {
            if (x <= -1 || x > OmokManager.OMOK_COUNT || y <= -1 || y > OmokManager.OMOK_COUNT)
            {
                return ERROR_CODE.OMOK_GAME_INVALIED_PACKET;
            }
            
            if (omok[y, x] != -1)
            {
                return ERROR_CODE.OMOK_GAME_INVALIED_POSITION;
            }
            
            omok[y, x] = userPos;
            
            return ERROR_CODE.NONE;
        }

        public bool ChkPointer(int x, int y, short userPos)
        {
            /*for (int j = 0; j < OmokManager.OMOK_COUNT; j++)
            {
                for (int i = 0; i < OmokManager.OMOK_COUNT; i++)
                {
                    Console.Write($"{omok[j, i] }");
                }
                Console.WriteLine();
            }*/
            
            int OMOKCOUNT = OmokManager.OMOK_COUNT;
            int _x = x;
            int _y = y;
            int count = 0;
            int user = userPos;
            
            
            // 가로 체크
            while (omok[_y, _x] == user && _x > 0)
            {
                _x--;
            }
            
            if (omok[_y, _x] == user && _x == 0)
            {
                _x = -1;
            }
            
            while (_x < OMOKCOUNT && omok[_y, ++_x] == user)
            {
                count++;
            }

            if (count >= 5)
            {
                MainServer.MainLogger.Info($"Game Over {user} winner");
                return true;
            }
            
            // 세로 체크
            _x = x;
            _y = y;
            count = 0;
            
            while (omok[_y, _x] == user && _y > 0)
            {
                _y--;
            }

            if (omok[_y, _x] == user && _y == 0)
            {
                _y = -1;
            }

            while (_y < OMOKCOUNT && omok[++_y, _x] == user)
            {
                count++;
            }

            if (count >= 5)
            {
                MainServer.MainLogger.Info($"Game Over {user} winner");
                return true;
            }
            
            // 오른쪽 아래 대각선
            _x = x;
            _y = y;
            count = 0;
            
            //while (omok[_y, _x] == user && _y > 0 && _x > 0)
            while (omok[_y, _x] == user && _y > 0 && _x >= 0)
            {
                _y--;
                _x++;
            }

            if (_x == 0 && omok[_y, _x] == user)
            {
                _y--;
                _x++;
            }

            while (_y < OMOKCOUNT && _x > 0 && omok[++_y, --_x] == user)
            {
                count++;
            }

            if (count >= 5)
            {
                MainServer.MainLogger.Info($"Game Over {user} winner");
                return true;
            }
            
            // 왼쪽 아래 대각선
            // chk 필요
            _x = x;
            _y = y;
            count = 0;
            
            /*while (omok[_y, _x] == user && _y > 0 && _x > 0)
            {
                _y--;
                _x++;
            }
            while (_y < OMOKCOUNT && _x < OMOKCOUNT && omok[_y++, _x--] == user)
            {
                count++;
            }*/

            MainServer.MainLogger.Info($"왼쪽 아래 대각선 체크하기 전 _x : {_x}, _y : {_y}");
            while (omok[_y, _x] == user && _y > 0 && _x > 0)
            {
                MainServer.MainLogger.Info($"체크 중 _x :{_x} && _y : {_y}");
                _y--;
                _x--;
            }

            if (omok[_y, _x] == user)
            {
                if (_y == 0 || _x == 0)
                {
                    _y--;
                    _x--;
                }
            }
            
            MainServer.MainLogger.Info($"number _x : {_x} , _y : {_y}");

            while (_y < OMOKCOUNT && _x < OMOKCOUNT && omok[++_y, ++_x] == user)
            {
                MainServer.MainLogger.Info($"counter chk _x : {_x}, _y : {_y}, count : {count}");
                count++;
            }
            
            MainServer.MainLogger.Info($"왼쪽 대각선 count : {count}");
            
            if (count >= 5)
            {
                MainServer.MainLogger.Info($"Game Over {user} winner");
                return true;
            }

            return false;
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

        public void NotifyPacketOmokTurn(int x, int y, short userPos)
        {
            var packet = new OMKNtfOmokTurn()
            {
                X = x,
                Y = y,
                UserPos = userPos
            };

            var body = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_OMOK_TURN, body);
            
            MainServer.MainLogger.Info($"NotifyPacketOmokGame : X : {x} Y : {y}");
            
            Broadcast(-1, sendPacket);
        }
        
        public void NotifyPacketOmokGame(short userPos)
        {
            var packet = new OMKNtfOmokGameRes()
            {
                Result = (short)ERROR_CODE.OMOK_GAME_RESULT_WIN,
                userPos = userPos
            };

            var body = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_OMOK_GAME_RES, body);
            
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
                
                NetSendFunc(user.NetSessionID, sendPacket);
            }
        }

        public int GetOtherUser(int excludeNetSessionIndex)
        {
            int otherUserindex = 0;
            foreach (var user in UserList)
            {
                if (user.NetSessionIndex == excludeNetSessionIndex)
                {
                    continue;
                }

                otherUserindex = user.NetSessionIndex;
            }

            return otherUserindex;
        }
    }
}