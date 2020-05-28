using System;

using CSBaseLib;
using MessagePack;

namespace ChatServer
{
    // Mainserver -> PacketProcessor -> ServerPacketData
    // BufferBlock의 Template 
    public class ServerPacketData
    {
        public Int16 PackSize;
        public string SessionID;
        public int SessionIndex;
        public Int16 PacketID;
        public SByte Type;
        public byte[] BodyData;

        // Q> 이 Assign 을 왜 하는거지? 어째서 방에 접속되어있는데 유저가 접속 해제 할 때에만 호출이 되는 걸까? 
        // A> 해당 유저가 방에 접속되어 있는 상태에 Server 연결 해제 요청이 들어오게 된다면 해당 함수가 호출된다
        // Assign함수는 PacketData 객체의 전역변수에 해당 유저의 Session 정보와 BodyData를 담아 객체화를 시킨다. 이는 어디에다 쓰이는 걸까?
        // Assign 함수가 실행 완료가 되면 PKHCommon에서 Receive Thread에 '해당 방에 유저만 떠나게 해주세요.' 라는 PacketID와 Assign에 의해 객체화된 PacketData를 등록한다
        // 이는 PKHRoom에서 해당 명령에 맞는 Event를 호출하고, 
        // 이 event함수는 SocketData를 매개변수로 갖게 되어 방에서 해당 유저를 삭제하게 된다.
        // 결국 이 함수는 방 속 유저를 삭제하기 위해 해당 유저 정보를 객체화 시키는 함수이다.
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

    [MessagePackObject]
    public class PKTInternalNTFRoomLeave
    {
        [Key(0)] public int RoomNumber;

        [Key(1)] public string UserID;
    }
}