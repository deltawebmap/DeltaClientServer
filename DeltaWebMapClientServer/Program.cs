using DeltaWebMapClientServer.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArkWebMapGatewayClient;
using ArkBridgeSharedEntities.Entities.NewSubserver;

namespace DeltaWebMapClientServer
{
    class Program
    {
        public static SystemLocalConfig setup;
        public static SystemRemoteConfig config;
        public static string token;

        public static HttpClient client = new HttpClient();
        public static AWMGatewayClient gateway;

        public static Dictionary<string, GameServer> servers;

        public const string DELTA_TOKEN_FILENAME = "delta.token";
        public const string DELTA_HEADLESS_TOKEN_FILENAME = "delta_setup_token.txt";

        static void Main(string[] args)
        {
            //Show splash
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"DeltaWebMap Client v{SystemVersion.SYSTEM_VERSION_MAJOR}.{SystemVersion.SYSTEM_VERSION_MINOR}");

            //Get config data
            if (!GetConfigs())
            {
                Console.ReadLine();
                return;
            }

            //Write more info
            Console.WriteLine($"Using {setup.enviornment}/{setup.mode}!");
            Console.ForegroundColor = ConsoleColor.White;

            //Write the startup message, if any
            if (config.startup_msg != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(config.startup_msg);
            }
            if(!config.startup_continue)
            {
                Console.ReadLine();
                return;
            }

            //Attempt to load this token
            if (File.Exists(DELTA_TOKEN_FILENAME))
                token = File.ReadAllText(DELTA_TOKEN_FILENAME);

            //Prompt for a token if one does not exist
            if(token == null)
                RequestActivateMachine();

            //Add token to the client
            client.DefaultRequestHeaders.Add("X-Machine-Token", token);

            //Connect to the gateway
            gateway = AWMGatewayClient.CreateClient(GatewayClientType.Subserver, "machine", setup.enviornment + "-" + setup.mode, SystemVersion.SYSTEM_VERSION_MAJOR, SystemVersion.SYSTEM_VERSION_MINOR, false, new GatewayHandler(), token);

            //Refresh server list
            RefreshServerList();

            //Just stop
            Task.Delay(-1).GetAwaiter().GetResult();
        }

        public static bool RequestActivateMachine()
        {
            //Prompt or read locally
            string shorthand;
            if(File.Exists(DELTA_HEADLESS_TOKEN_FILENAME))
            {
                //We're running headless
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Running headless configuration...");
                while (true)
                {
                    shorthand = File.ReadAllText(DELTA_HEADLESS_TOKEN_FILENAME);
                    if (TryMachineActivation(shorthand))
                        break;
                    else
                    {
                        Console.WriteLine($"Oops! The setup code '{shorthand}' didn't work. Make sure that the setup page is still open and try again. Press enter to reload the file '{DELTA_HEADLESS_TOKEN_FILENAME}'.");
                        Console.ReadLine();
                    }
                }
            } else
            {
                //We're asking for the code
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Welcome! Please type your setup code exactly as you see it below, then press enter.");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("If you're having trouble typing your setup code, follow the headless instructions.");
                Console.WriteLine("If you don't have a setup code, get started at https://deltamap.net/.");
                while(true)
                {
                    //Read input
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("> ");
                    shorthand = Console.ReadLine();
                    Console.ForegroundColor = ConsoleColor.Red;

                    //Now, attempt to commit our settings
                    if (TryMachineActivation(shorthand.Trim(' ')))
                        break;
                    else
                        Console.WriteLine($"Oops! Code '{shorthand}' didn't work. Make sure that the setup page is still open and try again.");
                }
            }

            //If we land here, we have activated successfully
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("You're ready to go! Head back to the configuration page on your computer.");
            return true;
        }

        public static bool TryMachineActivation(string shorthand)
        {
            //Create data to send
            MachineActivationPayload payload = new MachineActivationPayload
            {
                enviornment = setup.enviornment,
                mode = setup.mode,
                version_major = SystemVersion.SYSTEM_VERSION_MAJOR,
                version_minor = SystemVersion.SYSTEM_VERSION_MINOR,
                shorthand_token = shorthand.Replace(" ", "").Replace("-", "")
            };
            string payloadString = JsonConvert.SerializeObject(payload);

            //Send
            ActivationResponse responseData;
            try
            {
                var response = client.PostAsync(Program.config.endpoint_activate_machine, new StringContent(payloadString)).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    throw new Exception();
                responseData = JsonConvert.DeserializeObject<ActivationResponse>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
            catch
            {
                Log.E("Activate-Machine", "Couldn't activate the machine due to a server failure. Try again later. Sorry about that!");
                return false;
            }

            //Set token and save to disk
            token = responseData.token;
            if (token != null)
                File.WriteAllText(DELTA_TOKEN_FILENAME, token);

            return responseData.ok;
        }

        /// <summary>
        /// Fetches new servers and sets them up.
        /// </summary>
        public static void RefreshServerList()
        {
            //We're going to query server info
            MachineQueryInfoResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<MachineQueryInfoResponse>(client.GetStringAsync(Program.config.endpoint_query_machine_info).GetAwaiter().GetResult()); 
            } catch
            {
                Log.E("Refresh-Server-List", "Failed to download server list. Check your internet connection and ensure this machine is still registered. Trying again shortly...");
                return;
            }

            //If this is at startup, servers is null. Log and create
            if(servers == null)
            {
                Log.I("Refresh-Server-List", $"Registered machine as {response.name} ({response.id})");
                Log.I("Refresh-Server-List", $"Setting up {response.servers.Count} servers...");
                servers = new Dictionary<string, GameServer>();
            }

            //Warn if there are no servers
            if (response.servers.Count == 0)
                Log.W("Refresh-Server-List", "There are no servers registered for this machine. This program is useless before servers are added!");

            //Loop through and add servers that are not already here
            foreach(var s in response.servers)
            {
                if (servers.ContainsKey(s.id))
                    continue;
                servers.Add(s.id, new GameServer(s));
                Log.I("Refresh-Server-List", $"Added server {s.name} ({s.id})");
            }

            //Check if any servers were removed
            List<string> removeQueue = new List<string>();
            foreach(var s in servers)
            {
                if (response.servers.Where(x => x.id == s.Key).Count() != 0)
                    continue;
                removeQueue.Add(s.Key);
                Log.I("Refresh-Server-List", $"Removed server {s.Value.info.name} {s.Value.info.id})");
            }
            foreach (var s in removeQueue)
                servers.Remove(s);
        }

        /// <summary>
        /// Downloads new config data
        /// </summary>
        /// <returns></returns>
        static bool GetConfigs()
        {
            //Load the local config file
            try
            {
                setup = JsonConvert.DeserializeObject<SystemLocalConfig>(File.ReadAllText("setup.json"));
            }
            catch
            {
                Log.E("Get-Configs", "Failed to load the local config file. Aborting!");
                return false;
            }

            //Now, download the remote config file
            try
            {
                string config_url = $"https://config.deltamap.net/{setup.enviornment}/subserver/{SystemVersion.SYSTEM_VERSION_MAJOR}/{SystemVersion.SYSTEM_VERSION_MINOR}/{setup.mode}.json";
                config = JsonConvert.DeserializeObject<SystemRemoteConfig>(client.GetStringAsync(config_url).GetAwaiter().GetResult());
            }
            catch
            {
                Log.E("Get-Configs", "Failed to download the remote config file. Check your internet connection and try again. Aborting!");
                return false;
            }

            return true;
        }
    }
}
