using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoServerEx
{
    // 패킷 데이터 구조
    public class PacketData
    {
        public NetworkSession session;
        public EFBinaryRequestInfo reqInfo;
    }

    // Int 형으로 되어 있는 Packet Id 상태값
    public enum PACKETID : int
    {
        REQ_ECHO = 101,// Request Echo ID = 101
    }

    // Main 에서 연결될 Handler 객체
    public class CommonHandler
    {
        // 에코니까 Request에 들어온 대로 Session에 넘겨줌
        public void RequestEcho(NetworkSession session, EFBinaryRequestInfo requestInfo)
        {
            // 패킷의 전체 크기 : body + header 
            var totalSize = (Int16)(requestInfo.Body.Length + EFBinaryRequestInfo.HEADER_SIZE);
            
            List<byte> dataSource = new List<byte>();
            dataSource.AddRange(BitConverter.GetBytes(totalSize));
            dataSource.AddRange(BitConverter.GetBytes((Int16)PACKETID.REQ_ECHO)); 
            dataSource.AddRange(new byte[1]); 
            dataSource.AddRange(requestInfo.Body);
            
            session.Send(dataSource.ToArray(), 0, dataSource.Count);
        }
    }

    public class PK_ECHO
    {
        public string msg;
    }
}