using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using Torch;
using Torch.Server.Views;
using Torch.Views;
using NLog;
using MyPlug = ISMP_Pluging.BasePluging;
using MyConf = ISMP_Pluging.Conf.MyConfig;
using ISMP_Pluging.Class;
using ISMP_Pluging.ViewChildren.Util;
using ISMP_WorkshopService;
using ISMP_WorkshopService.Ext;
using ISMP_WorkshopService.Content;
using VRage.Filesystem;
using VRage.FileSystem;

namespace ISMP_Pluging.ViewChildren
{
    /// <summary>
    /// Logique d'interaction pour ViewWindowWorshop.xaml
    /// </summary>
    public partial class ViewWindowWorshop : Window
    {
        public MyPlug Pluging;
        public MyConf Config;
        public DownloadStatus Status;
        private static readonly Logger Log = LogManager.GetLogger("ISMP_Pluging @WWorkshop");
        public ViewWindowWorshop()
        {
            InitializeComponent();
            Status = new DownloadStatus();
            DataContext = Status;
            Pluging = MyPlug.Instance;
            Config = Pluging.Config;

            Status.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { UpdateLayout(); };
            AllowsTransparency = false;

            ThemeControl.UpdateDynamicControls += new Action<ResourceDictionary>(UpdateResourceDict);
            UpdateResourceDict(ThemeControl.currentTheme);
        }
        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dictionary);
        }


        public async void WorshopUserAddID_Click(object sender, RoutedEventArgs args)
        {
            Status.StatusMessage = "";
            Status.IsInProgress = true;
            var shouldClose = false;
            var delay = 5000;
            string statusMsg;
            ulong Woo = await MyPlug.Instance.TryParseAsync(WorshopUserTextEntryID.Text);


            if (!ulong.TryParse($"{Woo}", out ulong workshopId))     //!($"{workshopId}".Contains("0")))
            {

                statusMsg = $"{workshopId} It's not an Valid Id";
                Log.Info($"{statusMsg}");
                delay = 500;
            }
            else
            {


                statusMsg = $"{workshopId}Get Info From Worshop";
                PublishedItemDetails publishedItemDetails = null;

                var taskGetinfo = Task.Run(async delegate
                {
                    publishedItemDetails = await MyPlug.Instance.GetPublishedItemDetailsAsync(workshopId);

                });
                await Task.WhenAll(taskGetinfo);

                if (await MyPlug.Instance.IsAnIngameScriptAsync(publishedItemDetails.Tags))
                {

                    statusMsg = "Error With Download is Not an Ingame Script";
                    Log.Warn($"{statusMsg}");
                    delay = 1000;
                }
                else
                {
                    if (await MyPlug.Instance.HasAnUrlAsync(publishedItemDetails.FileUrl))//download by SteamWorshopService
                    {
                        statusMsg = "File have Url";
                        Log.Info($"  {statusMsg}");
                        //delay = 1000;
                        if (!await MyPlug.Instance.DownloadBySteamWoshopServiceAsync(publishedItemDetails.FileUrl, $"{workshopId}"))
                        {
                            statusMsg = "Error With Download";
                            delay = 1000;
                        }
                        else
                        {

                            await MyPlug.Instance.Addtolist(publishedItemDetails);
                            statusMsg = $"Succufully Downloaded {publishedItemDetails.Title}";
                            delay = 1000;
                            shouldClose = true;
                        }
                    }
                    else//Bypass SteamCMD
                    {
                        statusMsg = "Fil No Have Url";
                        Log.Warn($"{statusMsg}");
                        //delay = 1000;
                        if (!await MyPlug.Instance.BypassBySteamCMDAsync(workshopId))
                        {
                            statusMsg = "Error with Download";
                            Log.Warn($"{statusMsg}");
                            delay = 1000;
                        }
                        else
                        {
                            await MyPlug.Instance.Addtolist(publishedItemDetails);
                            statusMsg = $"Succufully Downloaded {publishedItemDetails.Title}";
                            delay = 1000;
                            shouldClose = true;
                        }
                    }
                }


            }

            Status.StatusMessage += "\n>" + statusMsg;

            await Task.Delay(delay);

            Status.IsInProgress = false;

            if (shouldClose)
            {
                Close();
            }
        }


    }
    public class DownloadStatus : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(StatusMessage)));
                }

            }
        }
        private bool _isInProgress = false;
        public bool IsInProgress
        {
            get => _isInProgress;
            set
            {
                if (_isInProgress != value)
                {
                    _isInProgress = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsInProgress)));
                }
            }
        }

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
