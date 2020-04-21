using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using NLog;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Compiler;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Network;
using VRageMath;
using VRage.Collections;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Commands;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Session;
using Torch.Views;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using Sandbox.Game;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Engine.Multiplayer;
using System.Security.Cryptography;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using Sandbox.Game.World;
using TheConfig = ISMP_Pluging.Conf.MyConfig;
using ISMP_Pluging.ViewChildren.Util;
using ISMP_Pluging.Class;
using ISMP_Pluging.Network;
using VRage.FileSystem;
using VRage.Filesystem;
using ISMP_WorkshopService.Content;
using ISMP_WorkshopService.Ext;
using ISMP_WorkshopService;
using System.Diagnostics;
using Sandbox.Game.GameSystems.TextSurfaceScripts;

namespace ISMP_Pluging
{

    public class BasePluging : TorchPluginBase, IWpfPlugin
    {
        public const string BASE_CONFIG = "IMSP2.cfg";
        public const string BASE_SCRIPT = "Scripts";
        public const string ERROR_FILE_CONFIG = "Configuration can't be Read Check Log for details ";
        public const string ERROR_FOLDER_SCRIPT = "Sripts Folder Error Check Log for details ";
        public const long MOD_ID = 2066935803;

        private static readonly Logger Log = LogManager.GetLogger("[ISMP_Pluging]@BasePluging > ");
        private TorchSessionManager _Tm;
        public UserControl Control;
        private Persistent<TheConfig> _config;

        public TheConfig Config => _config?.Data;
        public UserControl GetControl() => Control ?? (Control = new ViewBase() { DataContext = Config, Pluging = this });
        public static string ScriptPath { get; set; }
        public static string BasePatch { get; set; }
        public static string DownloadPatchCMD { get; set; }
        public static string SteamCmdDirectoryPatch { get; set; }
        public static string ConfigPatch { get; private set; }
        public static BasePluging Instance { get; private set; }

        public bool IsServerRunning
        {
            get
            {
                return _Tm.CurrentSession.State != TorchSessionState.Unloaded;
            }
        }
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            BasePatch = StoragePath.Replace("Instance", "");
            ConfigPatch = Path.Combine(StoragePath, BASE_CONFIG);
            ScriptPath = Path.Combine(StoragePath, $"{BASE_SCRIPT}");
            DownloadPatchCMD = ScriptPath;
            SteamCmdDirectoryPatch = $"{BasePatch}steamcmd";

            _config = Persistent<TheConfig>.Load(Path.Combine(StoragePath, BASE_CONFIG));

            bool IsMyConfigLoaded = (_config != null) ? true : false;

            bool ScriptPathExist = (MyFileSystem.DirectoryExists(ScriptPath)) ? true : false;
            if (!IsMyConfigLoaded || !ScriptPathExist)
            {
                if (!IsMyConfigLoaded)
                {
                    Log.Error($"{ERROR_FILE_CONFIG}");
                }

                if (!ScriptPathExist)
                {
                    try
                    {
                        MyFileSystem.CreateDirectoryRecursive($"{ScriptPath}");
                    }
                    catch
                    {
                        Log.Error($"{ERROR_FOLDER_SCRIPT}");
                    }

                }

            }

            Instance = this;
            _Tm = Torch.Managers.GetManager<TorchSessionManager>();
            if (_Tm != null) { _Tm.SessionStateChanged += OnSessionChanged; }

            var pathManager = torch.Managers.GetManager<PatchManager>();
            var patchContext = pathManager.AcquireContext();
            PatchSession(patchContext);
            PatchPB(patchContext);
            pathManager.Commit();


            Task.Run(delegate
            {
                foreach (var script in _config.Data.WhiteListScripts)
                {
                    string code = "";
                    code = script.Code;
                    script.MD5Hash = Utility.GetMD5Hash(code);
                }
            });

            MessageHandler.Init();
        }
        public async Task<PublishedItemDetails> GetPublishedItemDetailsAsync(ulong WorkShopID)
        {
            var WorshopService = WebAPI.Instance;
            var ScriptData = (await WorshopService.GetPublishedFileDetailsAsync(new ulong[] { WorkShopID }))?[WorkShopID];
            string _statusMsg;
            if (ScriptData == null)
            {
                _statusMsg = $"unable to det detail from {WorkShopID}";
                Log.Error(_statusMsg);
                return null;
            }
            else
            {
                if (ScriptData.ConsumerAppId != Utility.AppID)
                {
                    _statusMsg = $"Unables to reach {ScriptData.FileName} it's for {ScriptData.ConsumerAppId} your is {Utility.AppID}";
                    Log.Error(_statusMsg);
                    return null;
                }
                else
                {
                    return ScriptData;
                }
            }


        }
        public async Task Addtolist(PublishedItemDetails itemDetails, bool FromModuleCommand = false)
        {
            Script script1 = new Script
            {
                //Code = (await MyPlug.Instance.Config.GetScriptsCodeAsync(_workshopId)),
                Enabled = false,
                Name = itemDetails.Title,
                Patch = $"{ScriptPath}\\{itemDetails.PublishedFileId}",
                WorkshopID = itemDetails.PublishedFileId,
                Deleted = false,
                TimeCreated = itemDetails.TimeCreated,
                LastUpdate = itemDetails.TimeUpdated,

            };
            if (FromModuleCommand)
            {
                if (!_config.Data.WhiteListScripts.Contains(script1))
                {
                    _config.Data.WhiteListScripts.Add(script1);//need manual update for layout
                }
            }
            else
            {
                if (!_config.Data.WhiteListScripts.Contains(script1))
                {
                    (Control.DataContext as TheConfig).WhiteListScripts.Add(script1);
                    //Control.UpdateLayout();
                }

            }

            await Task.Delay(150);
        }
        public async Task<ulong> TryParseAsync(string _worshopID)
        {
            await Task.Delay(50);

            if (!ulong.TryParse(_worshopID, out ulong workshopId))
            {
                Log.Error($"ERROR Submit ID {_worshopID} it's not an good ID out {workshopId}");
                return 0L;
            }
            else
            {
                Log.Info($"TryParse Succes Submit ID {_worshopID} out {workshopId}");
                return workshopId;
            }
        }

        /*
         * Two Ways For download First by simple Post Method (DownloadBySteamWoshopServiceAsync) For Getting PublishedDetails, if URL have found he donwload it
         * Sometime no url fond so
         * If no Url found Second Method call with (BypassBySteamCMDAsync) 
         */
        public async Task<bool> DownloadBySteamWoshopServiceAsync(string fileUrl, string fileWorshopID)
        {
            var _workShopService = WebAPI.Instance;

            if (!await _workShopService.DownloadPublishFileAsync(fileUrl, ScriptPath, fileWorshopID))
            {
                return false;
            }
            else
            {
                return true;
            }


        }
      
        /*
         * Using Steam Cmd for download item with anonymous login
         */
        public async Task<bool> BypassBySteamCMDAsync(ulong WorkShopID)
        {

            // DownloadPatch = $"{ ScriptPath}steamapps\\workshop\\content\\{ Utility.AppID}\\";
            string DownloadPatch = $"{BasePatch}steamapps\\workshop\\content\\{Utility.AppID}\\{WorkShopID}"; //$"{MyPlug.DownloadPatchCMD}{WorkShopID}";
            string SteamCmdPatch = SteamCmdDirectoryPatch;


            string RunScript = $"{SteamCmdPatch}\\worshopDownload.txt";
            string[] param = { "@ShutdownOnFailedCommand 1 ", "@NoPromptForPassword 1", $"force_install_dir ../", "login anonymous", $"workshop_download_item {Utility.AppID} {WorkShopID}", "quit" };
            //create a file each time a new submit commit
            File.WriteAllLines(RunScript, param);

            var steamCMD = Task.Run(delegate
            {

                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                ProcessStartInfo startInfo = processStartInfo;
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = "steamcmd.exe";
                startInfo.WorkingDirectory = SteamCmdPatch;
                startInfo.Arguments = $"+runscript {RunScript}";

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }

            });

            await Task.WhenAll(steamCMD, Task.Delay(5000));
            /*
             * When Download With SteamCMD Folder it's not the same so
             * We Found requested ID in other folder and move it to The Instance folder !
             */
            DirectoryInfo di = new DirectoryInfo(DownloadPatch);
            {

                foreach (FileInfo files in di.GetFiles())
                {

                    if (files.Name.Contains(".cs"))
                    {
                        string _out = System.IO.Path.Combine($"{ScriptPath}\\{WorkShopID}", "Script.cs");
                        if (!Directory.Exists(System.IO.Path.Combine($"{ScriptPath}", $"{WorkShopID}")))
                        {
                            Directory.CreateDirectory(System.IO.Path.Combine($"{ScriptPath}", $"{WorkShopID}"));
                        }

                        if (File.Exists(_out))
                        {
                            foreach (var script in _config.Data.WhiteListScripts)
                            {
                                if (script.WorkshopID.Equals(WorkShopID))
                                {
                                    _config.Data.WhiteListScripts.Remove(script);
                                    break;
                                }
                            }
                            files.CopyTo(_out, true);
                        }
                        else
                        {
                            files.CopyTo(_out, true);
                        }
                    }

                }
            }
            return true;
        }
        public async Task<bool> IsAnIngameScriptAsync(string[] itemTags)
        {

            bool _IsIn = false;

            await Task.Delay(50);

            foreach (string tag in itemTags)
            {
                if (!tag.Contains("ingameScript"))
                {
                    _IsIn = true;
                }
                else
                {
                    _IsIn = false;
                }

            }
            return _IsIn;

        }
        public async Task<bool> HasAnUrlAsync(string fileUrl)
        {
            await Task.Delay(50);
            if (string.IsNullOrEmpty(fileUrl))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public async Task<bool> InteractWithScriptFromGameAsync(string scriptName, bool enable = true)
        {
            ulong targetId = await TryParseAsync(scriptName);
            Log.Info($"Submited For Enabled {targetId}");

            if (targetId.ToString() != "0")
            {
                bool found = false;
                if (enable)
                {
                    foreach (var script in _config.Data.WhiteListScripts)
                    {
                        if (script.WorkshopID == targetId)
                        {
                            script.Enabled = true;
                            found = true;
                            Log.Info($"{found}");

                        }
                    }
                    return found;
                }
                else
                {
                    foreach (var script in _config.Data.WhiteListScripts)
                    {
                        if (script.WorkshopID == targetId)
                        {
                            script.Enabled = false;
                            found = true;
                            Log.Info($"{found}");

                        }
                    }
                    return found;

                }
            }
            else
            {
                return false;
            }
        }
        public async Task<bool> RemoveScriptFromGameAsync(string scriptWorkshopID)
        {
            foreach (var item in _config.Data.WhiteListScripts)
            {
                if (item.WorkshopID.ToString() == scriptWorkshopID)
                {
                    if (item.Enabled == true)
                    {
                        return false;
                    }
                    else
                    {
                        _config.Data.WhiteListScripts.Remove(item);
                    }

                }
            }

            try
            {
                DirectoryInfo di = new DirectoryInfo(scriptWorkshopID);
                foreach (DirectoryInfo infoDir in di.GetDirectories())
                {
                    if (infoDir.Name.Contains(scriptWorkshopID))
                    {
                        di.Delete();
                    }
                }
                await Task.Delay(50);
            }
            catch (Exception error)
            {
                Log.Warn($"{error.Message}");
                return false;
            }
            return true;

        }


        static public void PatchSession(PatchContext context)
        {
            var sessionGetWorld = typeof(MySession).GetMethod(nameof(MySession.GetWorld));
            if (sessionGetWorld == null)
            {
                throw new InvalidOperationException("Couldn't find Method From MySessionGetWorld");
            }
            else
            {
                context.GetPattern(sessionGetWorld).Suffixes.Add(typeof(BasePluging).GetMethod(nameof(SuffixGetWorld), BindingFlags.Static | BindingFlags.NonPublic));
            }

        }
        static private void SuffixGetWorld(ref MyObjectBuilder_World __result)
        {
            //copy this list so mods added here don't propagate up to the real session
            __result.Checkpoint.Mods = __result.Checkpoint.Mods.ToList();

            if (Instance.Config.EnabledPlugin)
            {
                __result.Checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(MOD_ID));
            }

        }
        static public void PatchPB(PatchContext context)
        {
            //var pbCreateInstance = typeof(MyProgrammableBlock).GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.NonPublic);
            var pbCompile = typeof(MyProgrammableBlock).GetMethod("Compile", BindingFlags.Instance | BindingFlags.NonPublic);

            if (pbCompile == null)
            { throw new InvalidOperationException("Couldn't find Compile"); }
            else
            {
                var checkWhiteListCompile = typeof(BasePluging).GetMethod("CheckWhitelistCompile");
                context.GetPattern(pbCompile).Prefixes.Add(checkWhiteListCompile);
            }

        }

        static public bool CheckWhitelistCompile(string program, string storage, bool instantiate, object __instance = null)
        {
            if (!Instance.Config.EnabledWhiteList || program == null || program == "") { return true; }


            if (!Instance.Config.EnabledWhiteList)
            {
                return false;
            }

            // exclude npc factions
            var pb = (__instance as MyProgrammableBlock);
            if (CanBypassWhitelist(pb)) { return true; }

            //program = program.Replace(" \r", "");
            var whitelist = Instance.Config.WhiteListScripts;
            //var runningScripts = Instance.Config.RunningScripts;
            var scriptHash = Utility.GetMD5Hash(program);
            var comparer = StringComparer.OrdinalIgnoreCase;

            if (Instance.Config.ResetScriptEnabled && comparer.Compare(scriptHash, ResetPBScript.MD5) == 0)
            {
                Log.Info($"Script '{ResetPBScript.Name}' found on whitelist! Compiling...");
                return true;
            }

            foreach (var wScript in whitelist)
            {
                if (wScript.Enabled && comparer.Compare(scriptHash, wScript.MD5Hash) == 0)
                {
                    Instance.Config.AddRunningScript(pb.EntityId, wScript);
                    Log.Info($"Script '{wScript.Name}' found on whitelist! Compiling...");
                    return true;
                }
            }
            //_instance?.SetDetailedInfo("Script is not whitelisted. Compilation rejected!");

            //MyMultiplayer.RaiseEvent<MyProgrammableBlock>(__instance, (MyProgrammableBlock x) => x.WriteProgramResponse, msg, default(EndpointId));

            var script = Instance.Config.RunningScripts.GetValueOrDefault(pb.EntityId);

            if (script != null && whitelist.Contains(script) && script.Enabled)
            {
                var verifyHash = Utility.GetMD5Hash(script.Code);

                // check that script is not already loaded (and compilation failed for other reasons)
                if (comparer.Compare(scriptHash, script.MD5Hash) != 0 && script.Code != null && comparer.Compare(verifyHash, script.MD5Hash) == 0)
                {
                    Log.Info($"PB '{pb.EntityId}' seems to be outdated, updating code (script = {script.Name})...");
                    (pb as IMyProgrammableBlock).ProgramData = script.Code;
                    return false;
                }
            }
            else if (script != null)
            {
                script?.ProgrammableBlocks.Remove(pb.EntityId);
                Instance.Config.RemoveRunningScript(pb.EntityId);
            }


            var msg = "Script is not whitelisted. Compilation rejected!";


            var setDetailedInfo = typeof(MyProgrammableBlock).GetMethod("SetDetailedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setDetailedInfo == null)
            {
                //throw new InvalidOperationException("method SetDetailedInfo could not be retrieved!");
                Log.Info(msg);
                Log.Info("Script hash was: <" + scriptHash + ">");
            }
            Task.Delay(500).ContinueWith(_ =>
            {
                setDetailedInfo.Invoke(__instance, new object[] { msg });
            });

            return false;
        }
        static private bool CanBypassWhitelist(IMyProgrammableBlock pb)
        {
            ulong clientID = MySession.Static.Players.TryGetSteamId(pb.OwnerId);
            var factionTag = pb.GetOwnerFactionTag();
            var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
            var shareMode = ((MyCubeBlock)pb).IDModule.ShareMode;
            if (MySession.Static.GetUserPromoteLevel(clientID) == MyPromoteLevel.Owner)//If Client is Same As Server !
            {
                Log.Info($"REQUEST FROM USER ID < {clientID} >  HAVE PERMISSION TO BYPASS WHITELIST [OWNER]");

                return true;
            }


            if (MySession.Static.GetUserPromoteLevel(clientID) == MyPromoteLevel.Admin)//If Client is Admin On Server !
            {

                Log.Info($"REQUEST FROM USER ID < {clientID} >  HAVE PERMISSION TO BYPASS WHITELIST [ADMIN]");
                return true;
            }

            if (MySession.Static.GetUserPromoteLevel(clientID) == MyPromoteLevel.Scripter)//If Client is Scripter On Server !
            {
                Log.Info($"REQUEST FROM USER ID < {clientID} >  HAVE PERMISSION TO BYPASS WHITELIST [SCRIPTER]");

                return true;
            }
            if (MySession.Static.GetUserPromoteLevel(clientID) == MyPromoteLevel.SpaceMaster)//If Client is Space master On Server !
            {
                Log.Info($"REQUEST FROM USER ID < {clientID} > HAVE PERMISSION TO BYPASS WHITELIST [SPACEMASTER] ");

                return true;
            }

            if (MySession.Static.GetUserPromoteLevel(clientID) == MyPromoteLevel.None)//Normal player
            {
                Log.Info($"REJECTED REQUEST FROM USER ID < {clientID} > NO HAVE PERMISSION TO BYPASS WHITELIST [NORMALPLAYER]");

                return false;
            }

            if (shareMode == MyOwnershipShareModeEnum.All)
            {
                return false;
            }

            if (!MySession.Static.Players.IdentityIsNpc(pb.OwnerId))
            {
                return false;
            }

            if (faction == null)
            {
                return true;
            }

            if (shareMode == MyOwnershipShareModeEnum.None || (faction.IsEveryoneNpc() && !faction.AcceptHumans))
            {
                return true;
            }

            return false;
        }

        private void OnSessionChanged(ITorchSession torchSession, TorchSessionState sessionState)
        {
            switch (sessionState)
            {
                case TorchSessionState.Loading:
                    Config.LoadRunningScriptsFromWorld();
                    break;

                case TorchSessionState.Loaded:
                    MessageHandler.SetupMessaging();
                    break;

                case TorchSessionState.Unloading:
                    Config.SaveRunningScriptsToWorld();
                    break;

                case TorchSessionState.Unloaded:
                    MessageHandler.TearDown();
                    break;
            }
        }
        public void Save()
        {
            _config.Save();
        }

    }
}