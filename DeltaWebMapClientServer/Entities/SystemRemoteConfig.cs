using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMapClientServer
{
    /// <summary>
    /// Config file downloaded from our server.
    /// </summary>
    public class SystemRemoteConfig
    {
        //STARTUP
        public string startup_msg; //Message shown at startup.
        public bool startup_continue; //If false, the start up will stop.

        //ENDPOINTS
        public string endpoint_query_machine_info;
        public string endpoint_echo_files;
        public string endpoint_echo_upload;
        public string endpoint_echo_refresh;
        public string endpoint_activate_machine;
    }
}
