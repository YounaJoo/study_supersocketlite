using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace OMKServer
{
    public class PacketProcessor
    {
        bool IsThreadRunning = false;
        private Thread ProcessThread = null;
        
        // 데이터 흐름에 대한 데이터를 저장하기 위한 버퍼를 제공한다. 
        // BoundedCapacity 보다 크게 쌓이면 블럭킹이 된다. Receive 쪽에서 처리하지 않아도 post에서 블럭킹 되지 않는다.
        BufferBlock<ServerPacketData> MsgBuffer = new BufferBlock<ServerPacketData>();
        
        UserManager UserMgr = new UserManager();
        
        List<Room> RoomList = new List<Room>();
        
        List<Omok> OmokList = new List<Omok>();
        
        Dictionary<int, Action<ServerPacketData>> PacketHandlerMap = new Dictionary<int, Action<ServerPacketData>>();
        PKHCommon CommonPacketHandler = new PKHCommon();
        PKHRoom RoomPacketHandler = new PKHRoom();
        PKHOmok OmokHandler = new PKHOmok();
        
        public void CreateAndStart(List<Room> roomList, MainServer mainServer)
        {
            var maxUserCount = MainServer.ServerOption.RoomMaxCount * MainServer.ServerOption.RoomMaxCount;
            // 접근할 수 있는 user의 Max 치 설정
            UserMgr.Init(maxUserCount);
            
            // Omok Init 
            // OmokInit(MAX_X, MAX_Y, MIN_X, MIN_Y, DIS);

            RoomList = roomList;

            // 패킷 핸들러 등록
            RegistPacketHandler(mainServer);

            IsThreadRunning = true;
            ProcessThread = new Thread(this.Process);
            ProcessThread.Start();
        }

        private void OmokInit(float maxX, float maxY, float minX, float minY, float dis)
        {
            /*float x = minX;
            float y = minY;
            float temp = 0.0f;*/

            for (float y = minY; y < maxY; y += dis)
            {
                for (float x = minX; x < maxX; x += dis)
                {
                    OmokList.Add(new Omok(x, y));
                }
            }
        }

        // Message를 Send 하고 있던 Thread Destroy
        public void Destroy()
        {
            IsThreadRunning = false;
            MsgBuffer.Complete();
        }

        // MainServer에서 Client로부터 받은 패킷(접속, 접속 해제, 요청)을 Buffer에 Insert --> Thread 처리 
        public void InsertPacket(ServerPacketData data)
        {
            MsgBuffer.Post(data);
        }

        public void RegistPacketHandler(MainServer serverNetwork)
        {
            CommonPacketHandler.Init(serverNetwork, UserMgr);
            CommonPacketHandler.RegistPacketHandler(PacketHandlerMap);

            RoomPacketHandler.Init(serverNetwork, UserMgr);
            RoomPacketHandler.SetRoomList(RoomList);
            RoomPacketHandler.RegistPacketHandler(PacketHandlerMap);
            
            OmokHandler.Init(serverNetwork, UserMgr);
            //OmokHandler.SetOmokList(OmokList, OMOK_COUNT);
            OmokHandler.RegistPacketHandler(PacketHandlerMap);
        }

        void Process()
        {
            while (IsThreadRunning)
            {
                try
                {
                    var packet = MsgBuffer.Receive();

                    // 정상 동작
                    if (PacketHandlerMap.ContainsKey(packet.PacketID))
                    {
                        // 유지보수를 위해 if-else가 아닌 Handler 사용
                        PacketHandlerMap[packet.PacketID](packet);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("세션 번호 {0}, PacketID {1}, 받은 데이터 크기 : {2}", packet.SessionID,
                            packet.PacketID, packet.BodyData.Length);
                    }
                }
                catch (Exception e)
                {
                    IsThreadRunning.IfTrue(() => MainServer.MainLogger.Error(e.ToString()));
                }
            }
        }
    }
}