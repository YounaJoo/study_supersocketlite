using System;

using SuperSocket.Common;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketEngine.Protocol;

namespace EchoServerEx
{
    // SuperSocket.SocketBase.Procotol의 BinaryRequestInfo 를 상속받는 클래스
    // 패킷에 대한 정보를 담고 있는 클래스(패킷 크기, ID, 데이터-body, 헤더 크기 등)
    public class EFBinaryRequestInfo : BinaryRequestInfo
    {
        // 패킷의 데이터 : 패킷 전체의 크기, 패킷 종류(Echo), 데이터
        public Int16 TotalSize { get; private set; }
        public Int16 PacketID { get; private set; }
        public SByte Value1 { get; private set; }

        public const int HEADER_SIZE = 5;

        public EFBinaryRequestInfo(Int16 totalSize, Int16 packetID, SByte value1, byte[] body) : base(null, body)
        {
            this.TotalSize = totalSize;
            this.PacketID = packetID;
            this.Value1 = value1;
        }
    }

    // SuperSocket에 ReceiveFilter를 재정의 하는 클래스
    // ReceiveFilter는, 패킷 크기, 헤더 등을 정의하고 패킷 헤더에 따라 패킷을 잘개 쪼개는 역할을 한다. 쪼개는건 SuperSocket이 알아서 해주어 Execute 한다.
    // TRequestInfo(=위에 생성한 EFBinaryRequestInfo) 추상 멤버를 갖고 있는 FixedHeader~를 상속받았기에 재정의 필요 
    public class ReceiveFilter : FixedHeaderReceiveFilter<EFBinaryRequestInfo>
    {
        // 파생 클래스(ReceiverFilter)의 인스턴스 할당. 
        // base 를 통해 기반 클래스(부모 클래스)에 접근 가능
        public ReceiveFilter() : base(EFBinaryRequestInfo.HEADER_SIZE)
        {
        }
        
        // Header의 Body Packet의 크기를 Get
        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            // BigEndian으로 Request가 들어올 경우 LitteleEndian으로 변환 
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(header, offset, 2);
            }
            var nBodySize = BitConverter.ToInt16(header, offset);
            
            // Request 헤더에 body Size + header Size 를 했기 때문에 Header Size를 뺀 후 return
            return nBodySize - EFBinaryRequestInfo.HEADER_SIZE;
        }

        // Request Spec 지정하여 객체 반환
        protected override EFBinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(header.Array, 0, EFBinaryRequestInfo.HEADER_SIZE);
            }
            
            // 매개변수 타입, 종류, 개수 등은 개발자가 커스텀 할 수 있다.
            return new EFBinaryRequestInfo(BitConverter.ToInt16(header.Array, 0), 
                    BitConverter.ToInt16(header.Array, 0 + 2),
                            (SByte)header.Array[4], 
                             bodyBuffer.CloneRange(offset, length));
        }
    }
}