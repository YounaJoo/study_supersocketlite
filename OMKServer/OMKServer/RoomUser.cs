namespace OMKServer
{
    public class RoomUser
    {
        public string UserID { get; private set; }
        public int NetSessionIndex { get; private set; }
        public string NetSessionID { get; private set; }
        //public short UserPos { get; private set; }

        public void Set(string userID, int netSessionIndex, string netSessionID)
        {
            this.UserID = userID;
            this.NetSessionIndex = netSessionIndex;
            this.NetSessionID = netSessionID;
        }
        
        /*public void setUserPos(short pos)
        {
            this.UserPos = pos; // 0 : 흑(방장), 1 : 백(일반), -1 : none
        }*/
    }
}