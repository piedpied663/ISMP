using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using TViews = Torch.Views;
using Torch.ViewModels;
using VRage.FileSystem;
using Sandbox.ModAPI;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NLog;
using MyPlug = ISMP_Pluging.BasePluging;
using ISMP_Pluging.ViewChildren.Util;
namespace ISMP_Pluging.Class
{
    public class Script : ViewModel
    {
        private static readonly Logger Log = LogManager.GetLogger("[ISMP_Pluging]@SCRIPT Pattern");

        private bool _keepUpdated;
        public bool KeepUpdated
        {
            get => _keepUpdated;
            set
            {
                _keepUpdated = value;
                //if (value)
                //    _needsUpdate = true;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int InstallCount
        {
            get => ProgrammableBlocks.Count;
        }

        private ObservableCollection<long> _programmableBlocks = new ObservableCollection<long>();

        [XmlIgnore]
        public ObservableCollection<long> ProgrammableBlocks
        {
            get => _programmableBlocks;
            set
            {
                SetValue(ref _programmableBlocks, value);
                OnPropertyChanged("InstallCount");
            }
        }
        public Script()
        {
            OnPropertyChanged();
            Id = GetValidId();

            ProgrammableBlocks.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
            {
                /*if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
                    foreach (long pbId in e.OldItems)
                        MyPlug.Instance.Config.RunningScripts.Remove(pbId);
                if (e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Add)
                    foreach (long pbId in e.OldItems)
                        MyPlug.Instance.Config.RunningScripts[pbId] = script;*/
                OnPropertyChanged("InstallCount");
            };

        }
        private ulong _worshopId;
        public ulong WorkshopID
        {
            get => _worshopId; set
            {
                _worshopId = value;
                OnPropertyChanged();
            }
        }
        private string _name;
        [TViews.Display(Description = "Script Name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private bool _enabled = false;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }

        private string _thispatch;
        public string Patch
        {
            get => _thispatch;
            set
            {
                _thispatch = value;
                OnPropertyChanged();
            }
        }
        private DateTime _timeCreated;
        public DateTime TimeCreated
        {
            get => _timeCreated;
            set
            {
                _timeCreated = value;
                OnPropertyChanged();
            }
        }
        private DateTime _lastUpdate;
        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set
            {
                _lastUpdate = value;
                OnPropertyChanged();
            }
        }
        private bool _delete = false;
        public bool Deleted
        {
            get => _delete;
            set
            {
                _delete = value;
                OnPropertyChanged();
            }
        }

        private string _md5Hash;
        [TViews.Display(Name = "MD5 Hash", Description = "MD5 Hash of the script's code.")]
        [XmlIgnore]
        public string MD5Hash
        {
            get => _md5Hash;
            set
            {
                SetValue(ref _md5Hash, value);
            }
        }
        [DefaultValue(-1)]
        private long _id = -1;

        private static List<long> assignedIds = new List<long>();
        public static bool loadingComplete = false;
        public long Id
        {
            get => _id;
            set
            {
                //Log.Info($"assigned script ids: [{string.Join(", ", assignedIds)}]; id: {value}");
                if (value == -1)
                    throw new Exception("Invalid script id! -1 is not a valid script id. Check your ISMP_Pluging.cfg for invalid values!");
                if (value == _id)
                {
                    if (!assignedIds.Contains(value))
                        assignedIds.Add(value);
                    return;
                }

                var code = Code;

                if (assignedIds.Contains(value))
                {
                    if (assignedIds.Contains(value))
                        Log.Warn($"A script with id {value} already exists! Current id ({_id}) will be kept.");
                    else
                    {
                        Log.Warn($"A script with id {value} already exists! Will retrieve new valid id isntead.");
                        _id = GetValidId();
                    }
                }
                else
                {
                    var pos = assignedIds.IndexOf(_id);
                    if (pos != -1)
                        assignedIds.RemoveAt(pos);
                    _id = value;
                    //Log.Info($"new nextId is {nextId}");
                }
                assignedIds.Add(_id);

                Code = code;
                MD5Hash = Utility.GetMD5Hash(Code);
                OnPropertyChanged();
            }
        }
        public bool ShouldSerializeCode()
        {
            return !loadingComplete;
        }
        [XmlIgnore]
        public string Code
        {
            get
            {

                //Path.Combine($"{MyPlug.ScriptPath}\\{WorkshopID}", $"script.cs");
                bool _nonNull = ($"{WorkshopID}" == "0") ? true : false;        //string.IsNullOrEmpty($"{WorkshopID}");
                if (!_nonNull)
                {
                    var scriptPath = GetScriptPath();
                    //Log.Info($"{scriptPath}");
                    if (MyFileSystem.FileExists(scriptPath))
                    {
                        var code = File.ReadAllText(scriptPath);
                        if (code == null)
                        {
                            Log.Warn($"Error reading script '{Name}' (Id: {Id})!");
                            return "";
                        }
                        return code;
                    }
                    else
                    {
                        if (Id != -1)
                        {
                            Log.Warn($"Script code could not be found for script '{Name}' (Id: {Id})!");
                        }
                        return "";
                    }
                }
                else
                {
                    return "";
                }

            }
            set
            {
                if (!loadingComplete)
                    return;

                if (value == null)
                {
                    Log.Warn("Received invalid value for script code!");
                    return;
                }

                var scriptPath = GetScriptPath();
                //var code = value.Replace("\r\n", " \n");
                Directory.CreateDirectory(Path.GetDirectoryName(scriptPath));
                File.WriteAllText(scriptPath, value);
                MD5Hash = Utility.GetMD5Hash(value);
                OnPropertyChanged();
                //UpdateRunning();
            }
        }
        private string GetScriptPath()
        {
            return Path.Combine($"{MyPlug.ScriptPath}\\{WorkshopID}", $"Script.cs");
        }
        private long GetValidId()
        {
            long id;
            for (id = 0; id <= long.MaxValue; id++)
            {
                if (!assignedIds.Contains(id))
                    return id;
            }
            throw new Exception("Can't assign id: Maximum number of scripts reached!");
        }
    }
}
