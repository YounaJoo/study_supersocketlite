using System.Collections.Generic;

namespace OMKServer
{
    public class RoomManager
    {
        List<Room> RoomsList = new List<Room>();

        public void CreateRooms()
        {
            var maxRoomCount = MainServer.ServerOption.RoomMaxCount;
            var startNumber = MainServer.ServerOption.RoomStartNumber;
            //var maxUserCount = MainServer.ServerOption.RoomMaxUserCount;
            var maxUserCount = 2;

            for (int i = 0; i < maxRoomCount; ++i)
            {
                var roomNumber = (startNumber + i);
                var room = new Room();
                room.Init(i, roomNumber, maxUserCount);
                
                RoomsList.Add(room);
            }
        }

        public List<Room> GetRoomsList()
        {
            return RoomsList;
        }
    }
}