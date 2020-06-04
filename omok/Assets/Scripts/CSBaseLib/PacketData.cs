using MessagePack; 
using System;
using System.Collections.Generic;

namespace CSBaseLib
{
    public class PacketDef
    {
        public const Int16 PACKET_HEADER_SIZE = 5;
        public const int MAX_USER_ID_BYTE_LENGTH = 16;
        public const int MAX_USER_PW_BYTE_LENGTH = 16;
        public const int SYS_PACKET_ID_DISCONNECT_FROM_SERVER = 1;

        public const int INVALID_ROOM_NUMBER = -1;
    }

    public class PacketToBytes
    {
        public static byte[] Make(PACKETID packetID, byte[] bodyData)
        {
            byte type = 0;
            // 패킷 아이디 --> 16비트 Int로 변환
            var pktID = (Int16)packetID;
            Int16 bodyDataSize = 0;
            if (bodyData != null)
            {
                // 매개변수로 받은 byte[]의 크기를 멤버변수에 할당
                bodyDataSize = (Int16)bodyData.Length;
            }
            // 패킷 전체 크기 = body + header
            var packetSize = (Int16)(bodyDataSize + PacketDef.PACKET_HEADER_SIZE);
            
            var dataSource = new byte[packetSize];
            // Buffer Class : 기본 형식의 배열을 조작하는 클래스
            // Buffer.BlockCopy : 특정 오프셋에서 시작하는 소스 배열에서 특정 오프셋에서 시작하는 대상 배열로 지정된 바이트 수를 복사(바이트 블록을 전체 복사)
            // Buffer.BlockCopy Argument : (소스 배열, 소스 배열에 대한 바이트 오프셋, 대상 배열, 대상 배열에 대한 바이트 오프셋, 복사할 바이트 수)
            Buffer.BlockCopy(BitConverter.GetBytes(packetSize), 0, dataSource, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(pktID), 0, dataSource, 2, 2);
            dataSource[4] = type;
            
            if (bodyData != null)
            {
                Buffer.BlockCopy(bodyData, 0, dataSource, 5, bodyDataSize);
            }

            // 패킷에 대한 바이트 수가 복사된 dataSource 반환 (패킷의 전체 사이즈 --> 패킷 ID --> 패킷 Body)
            // Q> dataSource 의 역할은?? --> Packet을 하나로 뭉치는 것?
            return dataSource;
        }

        public static Tuple<int, byte[]> ClientReceiveData(int recvLength, byte[] recvData)
        {
            var packetSize = BitConverter.ToInt16(recvData, 0);
            var packetID = BitConverter.ToInt16(recvData, 2);
            var bodySize = packetSize - PacketDef.PACKET_HEADER_SIZE;

            var packetBody = new byte[bodySize];
            Buffer.BlockCopy(recvData, PacketDef.PACKET_HEADER_SIZE, packetBody,  0, bodySize);

            return new Tuple<int, byte[]>(packetID, packetBody);
        }
    }

    // 로그인 요청
    [MessagePackObject]
    public class OMKReqLogin
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string AuthToken;
    }

    [MessagePackObject]
    public class OMKResLogin
    {
        [Key(0)]
        public short Result;
    }


    [MessagePackObject]
    public class OMKMustClose
    {
        [Key(0)]
        public short Result;
    }

    [MessagePackObject]
    public class PKTReqRoomEnter
    {
        [Key(0)]
        public int RoomNumber;
    }

    [MessagePackObject]
    public class OMKResRoomEnter
    {
        [Key(0)]
        public short Result;
    }

    [MessagePackObject]
    public class OMKRoomUserList
    {
        [Key(0)]
        public List<string> UserIDList = new List<string>();
    }

    [MessagePackObject]
    public class OMKRoomNewUser
    {
        [Key(0)]
        public string UserID;
    }


    [MessagePackObject]
    public class OMKReqRoomLeave
    {
    }

    [MessagePackObject]
    public class OMKResRoomLeave
    {
        [Key(0)]
        public short Result;
    }

    [MessagePackObject]
    public class OMKRoomLeaveUser
    {
        [Key(0)]
        public string UserID;
    }


    [MessagePackObject]
    public class OMKReqRoomChat
    {
        [Key(0)]
        public string ChatMessage;
    }

    
    [MessagePackObject]
    public class OMKResRoomChat
    {
        [Key(0)]
        public string UserID;

        [Key(1)]
        public string ChatMessage;
    }
}