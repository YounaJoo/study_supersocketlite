using System;

namespace OMKServer
{
    public class User
    {
        private UInt32 SequenceNumber = 0;
        private string SessionID;
        private int SessionIndex = -1;
        public short UserPos { get; private set; } = -1;
        public int RoomNumber { get; private set; } = -1;
        private string UserID;

        public void Set(UInt32 sequence, string sessionID, int sessionIndex, string userID)
        {
            this.SequenceNumber = sequence;
            this.SessionID = sessionID;
            this.SessionIndex = sessionIndex;
            this.UserID = userID;

            setUserPos(-1);
        }

        public bool IsConfirm(string netSessionID)
        {
            return this.SessionID == netSessionID;
        }

        public string ID()
        {
            return this.UserID;
        }
        
        public void setUserPos(short pos)
        {
            this.UserPos = pos; // 0 : 흑(방장), 1 : 백(일반), -1 : none
        }

        public void EnteredRoom(int roomNumber)
        {
            this.RoomNumber = roomNumber;
        }

        public void LeaveRoom()
        {
            RoomNumber = -1;
            setUserPos(-1);
        }

        public bool IsStateLogin()
        {
            return this.SessionIndex != -1;
        }

        public bool IsStateRoom()
        {
            return this.RoomNumber != -1;
        }
    }
}