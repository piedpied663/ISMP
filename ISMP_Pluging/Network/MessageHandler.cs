﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using Sandbox.ModAPI;
using NLog;
using ISMP_ClientMod.Requests;
using ISMP_ClientMod.Conf;
using Sandbox.Game.EntityComponents;
using ISMP_Pluging.ViewChildren;
using System.ComponentModel;
using MyPlug = ISMP_Pluging.BasePluging;
using ISMP_Pluging.Class;
namespace ISMP_Pluging.Network
{
    public static class MessageHandler
    {


        private static readonly Logger Log = LogManager.GetLogger("[ISMP_Pluging]@MessageHandler");
        private static bool initialized = false;
        private static bool messagingReady = false;
        private static Dictionary<long, string> m_scripts = new Dictionary<long, string>();

        public static void Init()
        {
            if (initialized)
                return;

            MyPlug.Instance.Config.ScriptMainChanged += OnScriptEntryUpdate;
            MyPlug.Instance.Config.WhitheListScripts.CollectionChanged += UpdateWhitelist;
            MyPlug.Instance.Config.PropertyChanged += OnResetScriptToggled;
            if (MyPlug.Instance.Config.ResetScriptEnabled)
            { m_scripts[ResetPBScript.Id] = ResetPBScript.Name; }

            foreach (var script in MyPlug.Instance.Config.WhitheListScripts)
                if (script.Enabled)
                {
                    m_scripts[script.Id] = script.Name;
                }

            initialized = true;

        }

        public static void SetupMessaging()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(Config.MessageHandlerID, ReceiveRemoteRequest);
            //MyAPIGateway.Utilities.RegisterMessageHandler(Config.MOD_ID, ReceiveWhitelist);
            messagingReady = true;
        }

        public static void TearDown()
        {
            messagingReady = false;
        }
        private static void ReceiveRemoteRequest(byte[] bytes)
        {
            RemoteRequest request;
            try
            {
                request = NetworkUtil.DeserializeRequest(bytes);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return;
            }
            if (request.RequestType == RemoteRequestType.CLIENT_REGISTRATION)
            {
                SendWhitelistToClient(request.Sender);
            }
            else if (request.RequestType == RemoteRequestType.RECOMPILE)
            {
                var recompileRequest = request as RecompileRequest;
                if (recompileRequest == null)
                {
                    Log.Error("Request Deserialization: No serialized data (expected RecompileRequest)!");
                    return;
                }
                RecompilePB(recompileRequest.PbId, recompileRequest.ScriptId);
            }
            else
            {
                Log.Warn(string.Format("Invalid request type '{0}' to server!", request.RequestType));
            }
        }

        public static void Broadcast(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var player in players)
            {
                var id = player.SteamUserId;
                if (id != MyAPIGateway.Multiplayer.MyId && id != MyAPIGateway.Multiplayer.ServerId)
                    NetworkUtil.SendToClient(id, payload);
            }
        }

        public static void SendWhitelistToClient(ulong clientId)
        {
            Log.Info("Sending whitelist to client '{0}'...", clientId);

            var request = new MergedListActionRequest(
                MyAPIGateway.Multiplayer.MyId,
                ListUpdateAction.ADD,
                m_scripts);
            NetworkUtil.SendToClient(clientId, request);
        }


        private static void RecompilePB(long pbId, long scriptId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var pb = MyAPIGateway.Entities.GetEntityById(pbId) as IMyProgrammableBlock;

            Log.Debug(string.Format("Received recompilation request for pb '{0}'", pb.CustomName));

            /*if (pb.Storage == null)
                pb.Storage = new MyModStorageComponent();
            pb.Storage[Config.GUID] = scriptId.ToString();*/

            if (scriptId == -1)
            {
                pb.ProgramData = "";
                return;
            }

            if (scriptId == ResetPBScript.Id)
            {
                pb.ProgramData = ResetPBScript.Code;
                return;
            }

            var script = MyPlug.Instance.Config.WhitheListScripts.FirstOrDefault((item) => item.Id == scriptId);
            if (script == null)
            {
                Log.Error("Invalid script id in recompilation request!");
                return;
            }
            pb.ProgramData = script.Code;
        }

        private static void UpdateWhitelist(object sender, NotifyCollectionChangedEventArgs e)
        {

            ListUpdateAction action = ListUpdateAction.ADD;
            Dictionary<long, string> scripts = new Dictionary<long, string>();

            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in e.OldItems)
                {
                    var script = (item as Script);
                    if (m_scripts.ContainsKey(script.Id))
                    {
                        m_scripts.Remove(script.Id);
                        scripts[script.Id] = script.Name;
                    }
                }
                action = ListUpdateAction.REMOVE;
            }
            else if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (var item in e.NewItems)
                {
                    var script = (item as Script);
                    if (script.Enabled)
                    {
                        m_scripts[script.Id] = script.Name;
                        scripts[script.Id] = script.Name;
                    }
                }
                action = ListUpdateAction.ADD;
            }
            else
            {
                return;
            }

            if (scripts.Count == 0)
                return;

            if (messagingReady)
            {
                var request = new MergedListActionRequest(MyAPIGateway.Multiplayer.MyId,action, scripts);
                Broadcast(request);
            }
        }

        private static void OnScriptEntryUpdate(object sender, PropertyChangedEventArgs e)
        {
            var script = sender as Script;
            if (e.PropertyName == nameof(Script.Name) || e.PropertyName == nameof(Script.Enabled))
            {
                if (!script.Enabled && m_scripts.ContainsKey(script.Id))
                {
                    Log.Info($"Script {script.Name} disabled, updating clients...");
                    UpdateWhitelist(MyPlug.Instance.Config.WhitheListScripts,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, script));
                    //scripts.Remove(script.Id);
                }
                else if (script.Enabled)
                {
                    Log.Info($"Script {script.Name} added or changed, updating clients...");
                    UpdateWhitelist(MyPlug.Instance.Config.WhitheListScripts,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, script));
                    //scripts[script.Id] = script.Name;
                }
            }

        }

        private static void OnResetScriptToggled(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(MyPlug.Instance.Config.ResetScriptEnabled))
            {
                var action = ListUpdateAction.REMOVE;
                if (MyPlug.Instance.Config.ResetScriptEnabled)
                    action = ListUpdateAction.ADD;

                bool isAlreadyActive = m_scripts.ContainsKey(ResetPBScript.Id);
                if ((isAlreadyActive && action == ListUpdateAction.ADD)
                    || (!isAlreadyActive && action == ListUpdateAction.REMOVE))
                    return;

                if (messagingReady)
                {
                    var request = new MergedListActionRequest(MyAPIGateway.Multiplayer.MyId,action, new Dictionary<long, string> { { ResetPBScript.Id, ResetPBScript.Name } });
                    Broadcast(request);
                }
            }

        }

    }
}
