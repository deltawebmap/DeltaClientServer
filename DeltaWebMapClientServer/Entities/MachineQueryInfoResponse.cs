using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMapClientServer.Entities
{
    public class MachineQueryInfoResponse
    {
        public string id;
        public string name;
        public string token;
        public List<MachineQueryInfoResponseServer> servers;
    }

    public class MachineQueryInfoResponseServer
    {
        public string id;
        public string name;
        public string token;
        public string icon;
        public MachineQueryInfoResponseServerSettings load_settings;
    }

    public class MachineQueryInfoResponseServerSettings
    {
        /// <summary>
        /// The pathname to the save directory. Always ends with / or \.
        /// </summary>
        public string save_pathname;

        /// <summary>
        /// The .ark file to load, relative to save_pathname. Example: "Extinction.ark"
        /// </summary>
        public string save_map_name;

        /// <summary>
        /// Path to the config name.
        /// </summary>
        public string config_pathname;
    }
}
