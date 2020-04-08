using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Network;
using VRage.Serialization;
using VRage.Library.Collections;
using ISMP_ClientMod.Conf;
using ISMP_ClientMod.Requests;

namespace ISMP_ClientMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public partial class ISMP_ClientModCore : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(Config.MessageHandlerID, ReceiveRemoteRequest);
            SendToServer(new RemoteRequest(MyAPIGateway.Multiplayer.MyId, RemoteRequestType.CLIENT_REGISTRATION));
            base.BeforeStart();
        }
        private static void ReceiveWhitelist(ListUpdateAction action, object data)
        {

            var receivedScripts = data as Dictionary<long, string>;
            if (receivedScripts == null)
            {
                ModLogger.Error("Received invalid whitelist data!");
                return;
            }

            ModLogger.Info(string.Format("Whitelist received from server ({0} scripts):", receivedScripts.Count));
            foreach (var script in receivedScripts)
            {
                if (action == ListUpdateAction.ADD)
                    WitheListData.Scripts[script.Key] = script.Value;
                else if (action == ListUpdateAction.REMOVE && WitheListData.Scripts.ContainsKey(script.Key))
                    WitheListData.Scripts.Remove(script.Key);
                ModLogger.Info($"    [{action}] {script.Value}");
            }
        }

        private static void ReceiveRemoteRequest(byte[] bytes)
        {
            ModLogger.Info("Received remote request...");
            RemoteRequest request = null;
            try
            {
                request = NetworkUtil.DeserializeRequest(bytes);
            }
            catch (Exception e)
            {
                ModLogger.Error(e.Message);
                return;
            }
            ModLogger.Info("Successfully deserialized RemoteRequest.");
            if (request.RequestType == RemoteRequestType.MERGEDLIST_ACTION)
            {
                var clientRequest = request as MergedListActionRequest;
                if (clientRequest == null)
                {
                    ModLogger.Error("No serialized data (expected WhitelistActionRequest)!");
                    return;
                }

                ReceiveWhitelist(clientRequest.MergedListAction, clientRequest.WhiteList);
            }
            else
            {
                ModLogger.Warning(string.Format("Invalid request type '{0}' to client!", request.RequestType));
            }
        }
        public static void SendToServer(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageToServer(Config.MessageHandlerID, bytes);
        }
        public static void RequestPBRecompile(IMyProgrammableBlock pb, long scriptId)
        {
            ModLogger.Info(string.Format("Requesting recompilation of {0} with scriptId {1}", pb.CustomName, scriptId));

            var request = new RecompileRequest(
                MyAPIGateway.Multiplayer.MyId,
                pb.EntityId,
                scriptId);
            SendToServer(request);
        }
    }
}
