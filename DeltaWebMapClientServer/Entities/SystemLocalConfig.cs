using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMapClientServer
{
    /// <summary>
    /// This file is included in the download.
    /// </summary>
    public class SystemLocalConfig
    {
        /// <summary>
        /// "prod" or "staging"
        /// </summary>
        public string enviornment;

        /// <summary>
        /// Just modifies the config downloaded. Usually CONSUMER or PROVIDER
        /// </summary>
        public string mode;
    }
}
