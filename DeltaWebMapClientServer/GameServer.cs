using DeltaWebMapClientServer.Entities;
using DeltaWebMapClientServer.Entities.Echo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DeltaWebMapClientServer
{
    /// <summary>
    /// Used to represent a game server.
    /// </summary>
    public class GameServer
    {
        public MachineQueryInfoResponseServer info;

        private HttpClient client;
        private Timer updateTimer;
        private bool isBusy = false;

        public GameServer(MachineQueryInfoResponseServer info)
        {
            this.info = info;

            client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Delta-Server-ID", info.id);
            client.DefaultRequestHeaders.Add("X-Delta-Server-Creds", info.token);

            updateTimer = new Timer();
            updateTimer.AutoReset = true;
            updateTimer.Interval = new TimeSpan(0, 1, 0).TotalMilliseconds;
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.Start();

            Update().GetAwaiter().GetResult();
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Update().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the system
        /// </summary>
        /// <returns></returns>
        public async Task Update()
        {
            if (isBusy)
                return;
            isBusy = true;
            try
            {
                //Download the file list first
                List<EchoUploadedFile> files = await GetFileList();

                //Begin updating files
                int updates = 0;

                //Update the game file
                if (await UpdateFileAsync(files, info.load_settings.save_pathname + info.load_settings.save_map_name, "game", ArkUploadedFileType.ArkSave))
                    updates++;

                //Loop through files in the save dir and upload needed ones
                string[] diskFiles = Directory.GetFiles(info.load_settings.save_pathname);
                foreach(var s in diskFiles)
                {
                    string filename = new FileInfo(s).Name;
                    if (s.EndsWith(".arktribe"))
                    {
                        if (await UpdateFileAsync(files, s, filename, ArkUploadedFileType.ArkTribe))
                            updates++;
                    }
                    if (s.EndsWith(".arkprofile"))
                    {
                        if (await UpdateFileAsync(files, s, filename, ArkUploadedFileType.ArkProfile))
                            updates++;
                    }
                }

                //Loop through files in the config dir
                diskFiles = Directory.GetFiles(info.load_settings.config_pathname);
                foreach (var s in diskFiles)
                {
                    string filename = new FileInfo(s).Name;
                    if (await UpdateFileAsync(files, s, filename, ArkUploadedFileType.GameConfigINI))
                        updates++;
                }

                //If there were updates, refresh the data
                if(updates > 0)
                {
                    Log.I("Server-Update", $"Files changed, updating ARK file for server {info.id}...");
                    HttpResponseMessage response;
                    try
                    {
                        response = await client.PostAsync(Program.config.endpoint_echo_refresh, new StringContent(""));
                    }
                    catch { throw new StandardError("Remote-Update", "Failed to submit to handler"); }
                    if (!response.IsSuccessStatusCode)
                        throw new StandardError("Remote-Update", "The server failed to handle the request.");
                }
            } catch (StandardError ex)
            {
                Log.E("Server-Update-" + ex.topic, $"{ex.msg} (Server {info.id})");
            } catch (Exception ex)
            {
                Log.E("Server-Update", $"Unexpected error: {ex.Message}{ex.StackTrace} (Server {info.id})");
            }
            isBusy = false;
        }

        /// <summary>
        /// Updates a file on the server. If the file changed, this returns true.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private async Task<bool> UpdateFileAsync(List<EchoUploadedFile> files, string filename, string name, ArkUploadedFileType type)
        {
            //Get the data if it exists
            EchoUploadedFile fileData = GetFileFromList(files, name, type);

            //Open a stream on this file
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

            //Hash this file
            string hash;
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] hashBytes = sha1.ComputeHash(fs);
                hash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
            }

            //Check if this file needs updating
            if(fileData != null)
            {
                if (fileData.sha1 == hash)
                    return false;
            }

            //This file needs updating. Compress and upload this file.
            fs.Position = 0;
            Log.I("Update-File", $"Updating {name}/{type.ToString()} ({MathF.Round(fs.Length / 1024)} KB) for server {info.id}...");
            HttpResponseMessage response;
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(ms, CompressionLevel.Optimal, true))
                {
                    //Compress
                    await fs.CopyToAsync(gz);
                }

                //Upload
                ms.Position = 0;
                StreamContent sc = new StreamContent(ms);
                sc.Headers.Add("X-Delta-Filename", name);
                sc.Headers.Add("X-Delta-File-Type", type.ToString());
                try
                {
                    response = await client.PostAsync(Program.config.endpoint_echo_upload, sc);
                }
                catch
                {
                    throw new StandardError("Update-File", $"Failed to upload updated file. {name}/{type.ToString()} ({MathF.Round(fs.Length / 1024)} KB)");
                }
            }
            

            if(!response.IsSuccessStatusCode)
                throw new StandardError("Update-File", $"Failed to upload updated file. Status code was not OK. {name}/{type.ToString()} ({MathF.Round(fs.Length / 1024)} KB)");

            return true;
        }

        /// <summary>
        /// Returns an uploaded file from the file list if it exists. If it doens't, this returns null.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private EchoUploadedFile GetFileFromList(List<EchoUploadedFile> files, string name, ArkUploadedFileType type)
        {
            var results = files.Where(x => x.type == type && x.name == name);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Downloads the file list
        /// </summary>
        /// <returns></returns>
        private async Task<List<EchoUploadedFile>> GetFileList()
        {
            List<EchoUploadedFile> response;
            try
            {
                response = JsonConvert.DeserializeObject<List<EchoUploadedFile>>(await client.GetStringAsync(Program.config.endpoint_echo_files));
            } catch
            {
                throw new StandardError("Server-Get-File-List", "Failed to download file list.");
            }
            return response;
        }
    }
}
