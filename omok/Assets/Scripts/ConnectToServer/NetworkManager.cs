using System.Collections;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CSBaseLib;

namespace ConnectToServer
{
    public class NetworkManager : MonoBehaviour
    {

        public static Socket socket = null;

        ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
        ConcurrentQueue<byte[]> recvQueue = new ConcurrentQueue<byte[]>();

        private PacketBufferManager packetBuffer = null;

        public bool IsConnected { get; private set; } = false;

        // 스레드 실행 플래그
        protected bool isRunThreadLoop = false;

        protected Thread threadHandle = null;

        private const int mutSize = 1000;

        public Action<string> debugPrintFunc;

        // 소켓 연결
        public bool connect(string ip, int port)
        {
            if (packetBuffer == null)
            {
                packetBuffer = new PacketBufferManager();
                packetBuffer.Init((mutSize * 8), PacketDef.PACKET_HEADER_SIZE, mutSize);
            }

            bool ret = false;

            try
            {
                IPAddress serverIP = IPAddress.Parse(ip);
                int serverPort = port;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(serverIP, serverPort));

                if (socket == null || socket.Connected == false)
                {
                    return false;
                }

                // 쓰레드 시작 함수
                ret = LaunchThread();
            }
            catch (Exception e)
            {
                socket = null;
                Debug.Log(e.Message);
            }

            if (ret == true)
            {
                IsConnected = true;
                debugPrintFunc("Connect 성공");
            }
            else
            {
                IsConnected = false;
                debugPrintFunc("Connect 실패");
            }

            return IsConnected;
        }

        // 연결 끊기
        public void disconnect()
        {
            IsConnected = false;

            if (socket != null)
            {
                isRunThreadLoop = false;

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
        }

        // 애플리케이션 레이어에서 호출해야 한다. 메인 스레드에서 호출한다
        public List<PacketData> GetPacket()
        {
            var packetList = new List<PacketData>();
            const Int16 PacketHeaderSize = PacketDef.PACKET_HEADER_SIZE;

            byte[] buffer = null;
            var result = Receive(out buffer);
            if (result == false)
            {
                return packetList;
            }

            if (buffer.Length > 1)
            {
                packetBuffer.write(buffer, 0, buffer.Length);

                while (true)
                {
                    var data = packetBuffer.read();
                    if (data.Count < 1)
                    {
                        return packetList;
                    }

                    var packet = new PacketData();
                    packet.DataSize = (UInt16) (data.Count - PacketHeaderSize);
                    packet.PacketID = BitConverter.ToUInt16(data.Array, data.Offset + 2);
                    packet.Type = (SByte) data.Array[(data.Offset + 4)];
                    packet.BodyData = new byte[packet.DataSize];
                    Buffer.BlockCopy(data.Array, (data.Offset + PacketHeaderSize), packet.BodyData, 0,
                        (data.Count - PacketHeaderSize));
                    packetList.Add(packet);
                }
            }
            else
            {
                // 서버에서 접속을 종료하였음을 알린다.
                var packet = new PacketData();
                packet.PacketID = PacketDef.SYS_PACKET_ID_DISCONNECT_FROM_SERVER;
                packetList.Add(packet);
            }

            return packetList;
        }
        
        // 한 개만 return 하기
        public PacketData getPacket()
        {
            var packet = new PacketData();
            const Int16 PacketHeaderSize = PacketDef.PACKET_HEADER_SIZE;

            if (IsConnected == false)
            {
                Debug.Log("접속xx");
                return packet;
            }

            byte[] buffer = null;
            var result = Receive(out buffer);
            if (result == false)
            {
                return packet;
            }

            if (buffer.Length > 1)
            {
                packetBuffer.write(buffer, 0, buffer.Length);
                
                var data = packetBuffer.read();
                if (data.Count < 1)
                {
                    return packet;
                }

                packet.DataSize = (UInt16) (data.Count - PacketHeaderSize);
                packet.PacketID = BitConverter.ToUInt16(data.Array, data.Offset + 2);
                packet.Type = (SByte) data.Array[(data.Offset + 4)];
                packet.BodyData = new byte[packet.DataSize];
                Buffer.BlockCopy(data.Array, (data.Offset + PacketHeaderSize), packet.BodyData, 0,
                    (data.Count - PacketHeaderSize));
            }
            else
            {
                // 서버에서 접속을 종료하였음을 알린다.
                packet.PacketID = PacketDef.SYS_PACKET_ID_DISCONNECT_FROM_SERVER;
            }

            return packet;
        }

        // 송신처리.
        public void Send(byte[] data)
        {
            sendQueue.Enqueue(data);
        }


        // 수신처리.
        bool Receive(out byte[] buffer)
        {
            return recvQueue.TryDequeue(out buffer);
        }

        // 스레드 실행 함수
        bool LaunchThread()
        {
            try
            {
                isRunThreadLoop = true;
                threadHandle = new Thread(new ThreadStart(Dispatch));
                threadHandle.Start();
            }
            catch (Exception e)
            {
                debugPrintFunc("Cannot launch thread : " + e.Message);
                return false;
            }

            return true;
        }

        void Dispatch()
        {
            debugPrintFunc("Dispatch thread started");

            while (isRunThreadLoop)
            {
                if (socket != null && IsConnected == true)
                {
                    // 송신처리
                    DispatchSend();

                    // 수신 처리
                    DispatchReceive();
                }

                Thread.Sleep(5);
            }

            debugPrintFunc("dispatch thread ended");
        }

        // 스레드 측 송신처리 .
        void DispatchSend()
        {
            try
            {
                // 송신처리.
                if (socket.Poll(0, SelectMode.SelectWrite))
                {
                    byte[] buffer = null;

                    if (sendQueue.TryDequeue(out buffer))
                    {
                        socket.Send(buffer, buffer.Length, SocketFlags.None);
                    }
                }
            }
            catch
            {
                return;
            }
        }

        // 스레드 측의 수신처리.
        void DispatchReceive()
        {
            // 수신처리.
            try
            {
                byte[] buffer = new byte[mutSize];

                while (socket.Poll(0, SelectMode.SelectRead))
                {
                    int recvSize = socket.Receive(buffer, buffer.Length, SocketFlags.None);
                    if (recvSize == 0)
                    {
                        var closedBuffer = new byte[1];
                        recvQueue.Enqueue(buffer);

                        debugPrintFunc("Disconnected recv from client.");
                        disconnect();
                    }
                    else if (recvSize > 0)
                    {
                        var recvData = new byte[recvSize];
                        Buffer.BlockCopy(buffer, 0, recvData, 0, recvSize);
                        recvQueue.Enqueue(recvData);
                    }
                }
            }
            catch
            {
                return;
            }
        }
    }

    public struct PacketData
    {
        public UInt16 DataSize;
        public UInt16 PacketID;
        public SByte Type;
        public byte[] BodyData;
    }
}