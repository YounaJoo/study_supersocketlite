using System;

namespace ConnectToServer
{
    public class PacketBufferManager
    {
        private int bufferSize = 0;
        private int readPos = 0;
        private int writePos = 0;

        private int headerSize = 0;
        private int maxPacketSize = 0;
        private byte[] packetData;
        private byte[] packetDataTemp;

        public bool Init(int size, int headerSize, int maxPacketSize)
        {
            if (size < (maxPacketSize * 2) || size < 1 || headerSize < 1 || maxPacketSize < 1)
            {
                return false;
            }

            this.bufferSize = size;
            this.packetData = new byte[size];
            this.packetDataTemp = new byte[size];
            this.headerSize = headerSize;
            this.maxPacketSize = maxPacketSize;

            return true;
        }

        public bool write(byte[] data, int pos, int size)
        {
            if (data == null || (data.Length < (pos + size)))
            {
                return false;
            }

            var remainBufferSize = bufferSize - writePos;

            if (remainBufferSize < size)
            {
                return false;
            }
            
            Buffer.BlockCopy(data, pos, packetData, writePos, size);
            writePos += size;

            if (nextFree() == false)
            {
                bufferRelocate();
            }

            return true;
        }

        public ArraySegment<byte> read()
        {
            var enableReadSize = writePos - readPos;

            if (enableReadSize < headerSize)
            {
                return new ArraySegment<byte>();
            }

            var packetDataSize = BitConverter.ToInt16(packetData, readPos);
            if (enableReadSize < packetDataSize)
            {
                return new ArraySegment<byte>();
            }
            
            var completePacketData = new ArraySegment<byte>(packetData, readPos, packetDataSize);
            readPos += packetDataSize;
            
            return completePacketData;
        }

        bool nextFree()
        {
            var enableWriteSize = bufferSize - writePos;

            if (enableWriteSize < maxPacketSize)
            {
                return false;
            }

            return true;
        }

        void bufferRelocate()
        {
            var enableReadSize = writePos - readPos;
            
            Buffer.BlockCopy(packetData, readPos, packetDataTemp, 0, enableReadSize);
            Buffer.BlockCopy(packetDataTemp, 0, packetData, 0, enableReadSize);

            readPos = 0;
            writePos = enableReadSize;
        }
    }
}