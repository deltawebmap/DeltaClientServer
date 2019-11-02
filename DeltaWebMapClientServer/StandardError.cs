using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMapClientServer
{
    public class StandardError : Exception
    {
        public string msg;
        public string topic;

        public StandardError(string topic, string msg)
        {
            this.msg = msg;
            this.topic = topic;
        }
    }
}
