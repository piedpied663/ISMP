using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using VRage.FileSystem;
using Sandbox.ModAPI;
using MyPlug = ISMP_Pluging.BasePluging;
using MyVBase = ISMP_Pluging.ViewBase;
using MyVChild = ISMP_Pluging.ViewChildren;
using ISMP_Pluging.Class;
using NLog;
using Torch;
using Torch.Views;
using Torch.ViewModels;
using Torch.Utils;
using Torch.Session;
using Torch.Server;
using Torch.Patches;
using Torch.Mod;
using Torch.Managers;
using Torch.Event;
using Torch.Commands;
using Torch.Collections;
using Torch.API;
using Torch.API.Managers;
using VRage.Game.ModAPI;
using ISMP_Pluging;
using ISMP_WorkshopService.Content;
namespace ISMP_Pluging.Conf
{
    public class MyConfig : ViewModel
    {
        private UserControl _control;
        private static readonly Logger Log = LogManager.GetLogger("[ISMP_Pluging]@MyConfig");
        public UserControl GetControl() => _control ?? (_control = new MyVBase() { DataContext = this, Pluging = MyPlug.Instance });
        private const string runningScriptsFileName = "ISMP_Pluging_ActiveScripts.xml";//NETOYAGE a FAIRE
        public MyConfig() : base()
        {
            WhiteListScripts.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add || args.Action == NotifyCollectionChangedAction.Replace)
                {
                    foreach (var st in args.NewItems)
                        (st as Script).PropertyChanged += (script, propertyC) =>
                        {
                            OnScriptMainChanged(script, propertyC);
                        };
                    OnPropertyChanged(nameof(WhiteListScripts));
                }
            };

        }
        [XmlIgnore]
        public PropertyChangedEventHandler ScriptMainChanged;

        [XmlIgnore]
        public Dictionary<long, Script> RunningScripts = new Dictionary<long, Script>();

        public void OnScriptMainChanged(object sender, PropertyChangedEventArgs e)
        {
            ScriptMainChanged?.Invoke(sender, e);
        }
        [XmlIgnore]
        private ObservableCollection<Script> _whiteListscript = new ObservableCollection<Script>();
        [Display(EditorType = typeof(EmbeddedCollectionEditor))]
        public ObservableCollection<Script> WhiteListScripts
        {
            get { return _whiteListscript; }
            set
            {
                SetValue(ref _whiteListscript, value);
            }
        }

        [XmlIgnore]
        public bool _enabledWhiteList = true;
        public bool EnabledWhiteList
        {
            get => _enabledWhiteList;
            set
            {
                SetValue(ref _enabledWhiteList, value);
            }
        }
        [XmlIgnore]
        public bool _enabledPlugin = true;
        public bool EnabledPlugin
        {
            get => _enabledPlugin;
            set
            {
                SetValue(ref _enabledPlugin, value);
            }
        }
        private bool _resetScriptEnabled = true;
        public bool ResetScriptEnabled
        {
            get => _resetScriptEnabled;
            set
            {
                SetValue(ref _resetScriptEnabled, value);
            }
        }
        public void LoadRunningScriptsFromWorld()
        {
            Log.Info("Loading running scripts from world...");
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(runningScriptsFileName, typeof(MyConfig)))
            {
                //Dictionary<long, long> runningScripts;

                Dictionary<long, long> runningScripts = null;
                try
                {
                    using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(runningScriptsFileName, typeof(MyConfig)))
                        runningScripts = MyAPIGateway.Utilities.SerializeFromBinary<Dictionary<long, long>>(reader.ReadBytes((int)reader.BaseStream.Length));

                    //runningScripts = MyAPIGateway.Utilities.SerializeFromXML<Dictionary<long, long>>(serialized);
                }
                catch (Exception e)
                {
                    Log.Warn($"Parsing running scripts failed: {e.Message}");
                    return;
                }

                foreach (var kvp in runningScripts)
                {
                    var script = WhiteListScripts.First(item => item.Id == kvp.Value);
                    AddRunningScript(kvp.Key, script);
                }
            }

        }

        public void AddRunningScript(long pbId, Script script)
        {
            if (RunningScripts.ContainsKey(pbId))
            {
                RunningScripts[pbId].ProgrammableBlocks.Remove(pbId);
            }
            if (!script.ProgrammableBlocks.Contains(pbId))
            {
                script.ProgrammableBlocks.Add(pbId);
            }

            RunningScripts[pbId] = script;
            SaveRunningScriptsToWorld();
        }

        public void RemoveRunningScript(long pbId)
        {
            if (RunningScripts.ContainsKey(pbId))
            {
                RunningScripts[pbId].ProgrammableBlocks.Remove(pbId);
                RunningScripts.Remove(pbId);
                SaveRunningScriptsToWorld();
            }
        }

        public void SaveRunningScriptsToWorld()
        {
            var running = new Dictionary<long, long>();

            Log.Info("Saving running scripts to world...");

            if (RunningScripts != null)
            {
                int i = 0;
                foreach (var kvp in RunningScripts)
                {
                    running[kvp.Key] = kvp.Value.Id;
                    i++;
                }
            }
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(running);

            using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(runningScriptsFileName, typeof(MyConfig))) { writer.Write(serialized); }
        }


    }
}
