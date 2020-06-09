namespace OMKServer
{
    public class PKHandler
    {
        protected MainServer ServerNetwork;
        protected UserManager UserMgr = null;

        public void Init(MainServer serverNetwork, UserManager userMgr)
        {
            this.ServerNetwork = serverNetwork;
            this.UserMgr = userMgr;
        }
    }
}