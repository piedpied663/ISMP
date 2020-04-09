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


        public void WhiteListScripts_TargetUpdated(object sender, DataTransferEventArgs e)
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
            Script target = (WhiteListTable.SelectedItem as Script);
            if (target != null)
            {
                if (target.Deleted)
                {
                    await Delete(target);
                    Log.Info($"On Delete Item In Merged Table");
                    (DataContext as TheConfig).WhiteListScripts.Remove(target);
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