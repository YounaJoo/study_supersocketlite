using System;
using System.Collections.Generic;

namespace ChatServer
{
    // MainServer 개발하다가 넘어옴
    public class RoomManager
    {
        List<Room> RoomsList = new List<Room>();

        public void CreateRooms()
        {
            var maxRoomCount = MainServer.ServerOption.RoomMaxCount;
            var startNumber = MainServer.ServerOption.RoomStartNumber;
            var maxUserCount = MainServer.ServerOption.RoomMaxUserCount;

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