using System;

using CSBaseLib;
using MessagePack;

namespace OMKServer
{
    public class ServerPacketData
    {
        public Int16 PackSize;
        public string SessionID;
        public int SessionIndex;
        public Int16 PacketID;
        public SByte Type;
        public byte[] BodyData;
        
        public void Assign(string sessionID, int sessionIndex, Int16 packetID, byte[] packetBodyData)
        {
            this.SessionIndex = sessionIndex;
            this.SessionID = sessionID;

            this.PacketID = packetID;

            if (packetBodyData.Length > 0)
            {
                BodyData = packetBodyData;
            }
        }

        // Packet에 클라이언트 연결, 연결해제 ID 부여
        public static ServerPacketData MakeNTFInConnectOrDisConnectClientPacket(bool isConnect, string sessionID, int sessionIdex)
        {
            // return 시킬 객체 선언
            var packet = new ServerPacketData();
            
            if (isConnect)
            {
                packet.PacketID = (Int32) PACKETID.NTF_IN_CONNECT_CLIENT;
            }
            else
            {
                packet.PacketID = (Int32) PACKETID.NTF_IN_DISCONNECT_CLIENT;
            }

            packet.SessionIndex = sessionIdex;
            packet.SessionID = sessionID;

            return packet;
        }
    }
    
    [MessagePackObject]
    public class PKTInternalReqRoonEnter
    {
        [Key(0)] public int RoomNumber;

        [Key(1)] public string UserID;
    }

    [MessagePackObject]
    public class PKTInternalResRoomEnter
    {
        [Key(0)] public ERROR_CODE Result;

        [Key(1)] public int RoomNumber;

        [Key(2)] public string UserID;
    }

    // '방 나가기' 버튼과 '연결 끊기'는 동일하게 작용
    [MessagePackObject]
    public class PKTInternalNTFRoomLeave
    {
        [Key(0)] public int RoomNumber;

        [Key(1)] public string UserID;
    }
}