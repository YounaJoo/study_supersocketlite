using System;

namespace OMKServer
{
    public class Omok
    {
        public float x { get; private set; }
        public float y { get; private set; }
        public bool isActivity { get; private set; } = false;
        
        public short result { get; private set; } = -1;

        public Omok(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public void setActivity(bool isActivity)
        {
            this.isActivity = isActivity;
        }
    }
}