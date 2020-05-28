using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace ChatServer
{
    // MainServer 구현하다가 넘어감
    public class PacketProcessor
    {
        // 패킷 프로세스 쓰레드가 실행중인지 아닌지 체크하는 변수
        bool IsThreadRunning = false;
        // 패킷 프로세스가 돌 쓰레드
        private Thread ProcessThread = null;
        
        // BufferBlock<T>(DetaflowBlockOptions) 에서 DataflowBlockOptions의 BoundedCapacity로 버퍼 가능 수 지정. 
        // BoundedCapacity 보다 크게 쌓이면 블럭킹이 된다.
        // Receive 쪽에서 처리하지 않아도 post에서 블럭킹 되지 않는다.
        // System.Threading.Tasks.Dataflow.BufferBlock Class : 데이터 흐름에 대한 데이터를 저장하기 위한 버퍼를 제공한다. 
        // (맨날 헷갈려서, 버퍼는 데이터 흐름의 속도 차이를 조정하기 위해 일시적으로 데이터를 저장시키는 장치) 
        BufferBlock<ServerPacketData> MsgBuffer = new BufferBlock<ServerPacketData>();
        
        UserManager UserMgr = new UserManager();
        
        // Tuple 
        Tuple<int, int> RoomNumberRange = new Tuple<int, int>(-1, -1);
        List<Room> RoomList = new List<Room>();
        
        Dictionary<int, Action<ServerPacketData>> PacketHandlerMap = new Dictionary<int, Action<ServerPacketData>>();
        PKHCommon CommonPacketHandler = new PKHCommon();
        PKHRoom RoomPacketHandler = new PKHRoom();
        
        public void CreateAndStart(List<Room> roomList, MainServer mainServer)
        {
            var maxUserCount = MainServer.ServerOption.RoomMaxCount * MainServer.ServerOption.RoomMaxCount;
            // 접근할 수 있는 user의 Max 치 설정
            UserMgr.Init(maxUserCount);

            // Q> (궁금증) 멀티쓰레드 서버 구성 시, 이런 복사를 많이 쓰지 말라고 하는데 C++에서는 어떻게 할까??
            RoomList = roomList;
            var minRoomNum = RoomList[0].Number;
            var maxRoomNum = RoomList[0].Number + RoomList.Count() - 1;
            RoomNumberRange = new Tuple<int, int>(minRoomNum, maxRoomNum);

            // 패킷 핸들러 등록(
            RegistPacketHandler(mainServer);

            IsThreadRunning = true;
            ProcessThread = new Thread(this.Process);
            ProcessThread.Start();
        }

        // Message를 Send 하고 있던 Thread Destroy
        public void Destroy()
        {
            IsThreadRunning = false;
            // IDataflowBlock에 대한 신호를 더 이상 메시지를 받거나 생성할 수 없게 만든다.
            MsgBuffer.Complete();
        }

        // MainServer에서 Client로부터 받은 패킷(접속, 접속 해제, 요청)을 Buffer에 Insert --> Thread 처리 
        public void InsertPacket(ServerPacketData data)
        {
            // MsgBuffer에 항목을 게시
            MsgBuffer.Post(data);
        }

        public void RegistPacketHandler(MainServer serverNetwork)
        {
            // 실행할 MainServer에 UserManager 추가 작업 초기화
            CommonPacketHandler.Init(serverNetwork, UserMgr);
            CommonPacketHandler.RegistPacketHandler(PacketHandlerMap);

            RoomPacketHandler.Init(serverNetwork, UserMgr);
            RoomPacketHandler.SetRoomList(RoomList);
            RoomPacketHandler.RegistPacketHandler(PacketHandlerMap);
        }

        void Process()
        {
            while (IsThreadRunning)
            {
                //Thread.Sleep(65);// 테스트 용 
                try
                {
                    // 지정된 소스에서 값을 동기적으로 받는다.
                    var packet = MsgBuffer.Receive();

                    // 정상 동작 : 해당 메세지 패킷의 종류(ID)가 핸들러에 매핑되어 있는 Key값에 포함된다면
                    if (PacketHandlerMap.ContainsKey(packet.PacketID))
                    {
                        // 해당 키에 대한 
                        PacketHandlerMap[packet.PacketID](packet);
                        
                        Console.Write($"데이터 정상적으로 받음 : {packet.PacketID}\n");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("세션 번호 {0}, PacketID {1}, 받은 데이터 크기 : {2}", packet.SessionID,
                            packet.PacketID, packet.BodyData.Length);
                    }
                }
                catch (Exception e)
                {
                    // IfTure() :: NuGet - Z.ExtendsionMethods Packages 추가
                    IsThreadRunning.IfTrue(() => MainServer.MainLogger.Error(e.ToString()));
                }
            }
        }
    }
}