using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch;
using Torch.Collections;
using Torch.ViewModels;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using NLog;
using System.Net.Http;
using ISMP_Pluging.ViewChildren;
using TheConfig = ISMP_Pluging.Conf.MyConfig;
using MyPlug = ISMP_Pluging.BasePluging;
using ISMP_Pluging.Class;
using VRage.FileSystem;
namespace ISMP_Pluging
{
    /// <summary>
    /// Logique d'interaction pour UserControl1.xaml
    /// </summary>
    /// 
    public partial class ViewBase : UserControl
    {

        public MyPlug Pluging;
        public TheConfig Config;
        public static Logger Log = LogManager.GetLogger("[IMSP@BaseView]");
        public ObservableCollection<Script> Items { get; set; }
        private static readonly object _syncLock = new object();
        public ViewBase() : base()
        {
            InitializeComponent();
            Pluging = MyPlug.Instance;
            Config = Pluging.Config;
            Items = new ObservableCollection<Script>();

            BindingOperations.EnableCollectionSynchronization(Items, _syncLock);
            DataContext = this;

        }


        public void WhitheListScripts_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            Pluging.Save();
        }

        public void Button_Click_OpenViewChildrenDownload(object sender, RoutedEventArgs args)
        {
            var _workshopWindows = new ViewWindowWorshop();
            _workshopWindows.Show();
        }
        public async void Button_Click_RemoveScriptAsync(object sender, RoutedEventArgs args)
        {
            Script target = (WhitheListTable.SelectedItem as Script);
                if (target != null)
                {
                    if (target.Deleted)
                    {
                        await Delete(target);
                        Log.Info($"On Delete Item In Merged Table");
                        (DataContext as TheConfig).WhitheListScripts.Remove(target);
                        Pluging.Save();
                        MessageBox.Show($"Sucefully removed {target.Name}");
                    }
                    else
                    {
                        Log.Info($"Merged Table Nothing Enable To Delete");
                        return;
                    }

                }
                else
                {
                    Log.Error($"Nothing to Do");
                    return;
                }

                await Task.Delay(100);
        }
        private async Task Delete(Script target)
        {
            string Patch = target.Patch;
            if (Patch.Contains("Script.cs"))
            {
                //Log.Warn($"{target.Patch}");
                //Log.Warn($"REPLACED {target.Patch.Replace("Script.cs", "")}");
                Patch = target.Patch.Replace("Script.cs", "");
            }

            DirectoryInfo di = new DirectoryInfo($"{Patch}");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            if (MyFileSystem.IsDirectory($"{di}"))
            {
                Directory.Delete($"{di}");
            }
            await Task.Delay(500);
            return;

        }
        public void Button_Click_SaveConf(object sender, RoutedEventArgs args)
        {
            Pluging.Save();
        }
        private void Button_Click_OpenFolderScript(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = $"{MyPlug.ScriptPath}",
                UseShellExecute = true,
                Verb = "open"
            });
            return;
        }
     
        private void Button_Click_OpenCfg(object sender, RoutedEventArgs e)
        {

            Process.Start(new ProcessStartInfo()
            {
                FileName = $"{MyPlug.ConfigPatch}",
                UseShellExecute = true,
                Verb = "open"
            });
            return;

        }
    }




}

/*
 * DEPRECIATED
 * 
        public void Button_Click_OpenFolderDownload(object sender, RoutedEventArgs args)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = $"{MyPlug.DownloadPatchCMD}",
                UseShellExecute = true,
                Verb = "open"
            });
            return;
        }
        private async void Button_Click_MergeScriptAsync(object sender, RoutedEventArgs e)
        {

            Script target = (WhitheListScripts_TargetUpdated.SelectedItem as Script);

            if (target != null && target.Enabled)
            {
                DirectoryInfo di = new DirectoryInfo($"{target.Patch}");
                {
                    foreach (FileInfo item in di.GetFiles())
                    {
                        if (item.Name.Contains(".cs"))
                        {
                            string _out = System.IO.Path.Combine($"{MyPlug.ScriptPath}\\{target.WorkshopID}", "Script.cs");
                            if (!Directory.Exists(System.IO.Path.Combine($"{MyPlug.ScriptPath}", $"{target.WorkshopID}")))
                            {
                                Directory.CreateDirectory(System.IO.Path.Combine($"{MyPlug.ScriptPath}", $"{target.WorkshopID}"));
                            }
                            if (File.Exists(_out))
                            {
                                MessageBoxResult result = MessageBox.Show($"File {target.Name} already exists in {MyPlug.ScriptPath}  Override IT ? ", "APP", MessageBoxButton.YesNo);
                                switch (result)
                                {
                                    case MessageBoxResult.Yes:
                                        foreach (var script in (DataContext as TheConfig).WhitheListScripts)
                                        {
                                            if (script.WorkshopID.Equals(target.WorkshopID))
                                            {
                                                (DataContext as TheConfig).WhitheListScripts.Remove(script);
                                                break;
                                            }
                                        }
                                        item.CopyTo(_out, true);
                                        await AddToMergedList(target);
                                        MessageBox.Show($"Succefully Merged {target.Name}", "APP");
                                        break;
                                    case MessageBoxResult.No:
                                        break;

                                }
                            }
                            else
                            {
                                item.CopyTo(_out, true);
                                MessageBox.Show($"Succefully Merged {target.Name}", "APP");
                                await AddToMergedList(target);
                                //(DataContext as TheConfig).MergedListScripts.Add(target as Script);
                            }
                            //Log.Info($"{System.IO.Path.Combine($"{MyPlug.ScriptPath}", "Script.cs")}"); GOOD
                            Log.Info($"{_out}");
                        }

                    }
                }
            }


            await Task.Delay(500);
            return;
        }
        
        public async Task AddToMergedList(Script target)
        {
            string _out = System.IO.Path.Combine($"{MyPlug.ScriptPath}\\{target.WorkshopID}", "Script.cs");

            Script script = new Script
            {
                Deleted = false,
                Enabled = false,
                Name = target.Name,
                Patch = _out,
                LastUpdate = target.LastUpdate,
                TimeCreated = target.TimeCreated,
                WorkshopID = target.WorkshopID,
            };
            if (!(DataContext as TheConfig).WhitheListScripts.Contains(script))
            {
                (DataContext as TheConfig).WhitheListScripts.Add(script);
            }

            await Task.Delay(500);
        }
    */
