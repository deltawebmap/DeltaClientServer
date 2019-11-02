using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ArkBridgeSharedEntities.Entities.NewSubserver;
using ArkWebMapGatewayClient.Messages.SubserverClient;
using Newtonsoft.Json;
using System.Net.Http;

namespace DeltaWebMapClientServer
{
    public class GatewayHandler : ArkWebMapGatewayClient.GatewayMessageHandler
    {
        public override void Msg_MessageDirListing(MessageDirListing data, object context)
        {
            string response = JsonConvert.SerializeObject(Tools.RemoteDirListTool.GetListing(data.pathname));
            StringContent sc = new StringContent(response);
            sc.Headers.Add("X-FileList-Token", data.token);
            Program.client.PostAsync(data.callback_url, sc);
        }

        public override void Msg_OnMachineUpdateServerList(MessageMachineUpdateServerList data, object context)
        {
            //Update the server list now.
            Log.I("Gateway-Handler", "Server list updated, pulling...");
            Program.RefreshServerList();
        }
    }
}
