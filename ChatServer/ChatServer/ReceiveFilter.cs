﻿using System;

using ChatServer;
using SuperSocket.Common;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketEngine.Protocol;

namespace ChatServer
{
    public class EFBinaryRequestInfo : BinaryRequestInfo
    {
        public Int16 Size { get; private set; }
        public Int16 PacketID { get; private set; }
        public SByte Type { get; private set; }

        public const int HEADER_SIZE = 5;

        public EFBinaryRequestInfo(Int16 size, Int16 packetID, SByte type, byte[] body) : base(null, body)
        {
            this.Size = size;
            this.PacketID = packetID;
            this.Type = type;
        }
    }
}
    
    public class ReceiveFilter : FixedHeaderReceiveFilter<EFBinaryRequestInfo>
    {
        public ReceiveFilter() : base(CSBaseLib.PacketDef.PACKET_HEADER_SIZE)
        {
        }

        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(header, offset, CSBaseLib.PacketDef.PACKET_HEADER_SIZE);
            }

            var pakcetSize = BitConverter.ToInt16(header, offset);
            var nBodySize = pakcetSize - CSBaseLib.PacketDef.PACKET_HEADER_SIZE;
            
            return nBodySize;
        }

        protected override EFBinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(header.Array, 0, CSBaseLib.PacketDef.PACKET_HEADER_SIZE);
            }
            
            return new EFBinaryRequestInfo(BitConverter.ToInt16(header.Array, 0), 
                BitConverter.ToInt16(header.Array, 2),
                (SByte)header.Array[4], 
                bodyBuffer.CloneRange(offset, length));
        }
    }