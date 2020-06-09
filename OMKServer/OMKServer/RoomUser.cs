namespace OMKServer
{
    public class RoomUser
    {
        public string UserID { get; private set; }
        public int NetSessionIndex { get; private set; }
        public string NetSessionID { get; private set; }

        public void Set(string userID, int netSessionIndex, string netSessionID)
        {
            this.UserID = userID;
            this.NetSessionIndex = netSessionIndex;
            this.NetSessionID = netSessionID;
        }
    }
}