using ArkBridgeSharedEntities.Entities.NewSubserver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaWebMapClientServer.Tools
{
    public static class RemoteDirListTool
    {
        public static RemoteDirListing GetListing(string pathname)
        {
            RemoteDirListing output;
            if (pathname == "")
            {
                //Get all drives
                string[] drives = Directory.GetLogicalDrives();
                List<RemoteDir> outputListings = new List<RemoteDir>();
                foreach (var d in drives)
                {
                    outputListings.Add(new RemoteDir
                    {
                        children = null,
                        children_ok = true,
                        pathname = d,
                        name = d,
                        type = "DRIVE"
                    });
                }
                output = new RemoteDirListing
                {
                    children = outputListings,
                    children_ok = true
                };
            }
            else
            {
                //Get dir
                var results = Helper_CreateRemoteDirListing(pathname, true);
                output = new RemoteDirListing
                {
                    children = results,
                    children_ok = results != null
                };
            }
            return output;
        }

        private static List<RemoteDir> Helper_CreateRemoteDirListing(string pathname, bool getChildren)
        {
            //Get files and dirs
            string[] files;
            string[] dirs;
            try
            {
                files = Directory.GetFiles(pathname);
                dirs = Directory.GetDirectories(pathname);
            }
            catch
            {
                return null;
            }

            //Loop through each
            List<RemoteDir> output = new List<RemoteDir>();
            foreach (var s in dirs)
            {
                RemoteDir l = new RemoteDir
                {
                    name = new DirectoryInfo(s).Name,
                    type = "DIRECTORY",
                    pathname = s,
                    children = null,
                    children_ok = true
                };
                if (getChildren)
                {
                    var results = Helper_CreateRemoteDirListing(s, false);
                    l.children = results;
                    l.children_ok = results != null;
                }
                output.Add(l);
            }
            foreach (var s in files)
            {
                RemoteDir l = new RemoteDir
                {
                    name = new FileInfo(s).Name,
                    type = "FILE",
                    pathname = s,
                    children = null,
                    children_ok = true
                };
                output.Add(l);
            }

            return output;
        }
    }
}
