using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMapClientServer.Entities.Echo
{
    public class EchoUploadedFile
    {
        /// <summary>
        /// The time this file was uploaded at.
        /// </summary>
        public long time_utc { get; set; }

        /// <summary>
        /// The filename on disk
        /// </summary>
        public string token { get; set; }

        /// <summary>
        /// The type of file stored
        /// </summary>
        public ArkUploadedFileType type { get; set; }

        /// <summary>
        /// The name of the file
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Size of the file (compressed)
        /// </summary>
        public long compressed_size { get; set; }

        /// <summary>
        /// The size of the decompressed data
        /// </summary>
        public long size { get; set; }

        /// <summary>
        /// Sha1 of the compressed data
        /// </summary>
        public string sha1 { get; set; }
    }

    public enum ArkUploadedFileType
    {
        ArkSave, //.ark in Saved
        ArkTribe, //.arktribe in Saved
        ArkProfile, //.arkprofile in Saved
        GameConfigINI //A game .ini config file
    }
}
