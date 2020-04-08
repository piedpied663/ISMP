using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Packaging;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
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
using System.Net.Http;
using System.Net;
using SteamKit2;
using ISMP_WorkshopService.Ext;
using ISMP_WorkshopService.Content;
using VRage.Filesystem;
using VRage.FileSystem;
using Sandbox.Game.Gui;

namespace ISMP_WorkshopService
{
    public class WebAPI
    {
        private static Logger Log = LogManager.GetLogger("[ISMP2@]SteamWorshopService");
        private const uint APPID = 24850U;
        public string Username { get; private set; }
        private string passwd;
        public bool IsReady { get; private set; }
        public bool IsRunning { get; private set; }
        private TaskCompletionSource<bool> logonTaskCompletionSource;
        private SteamClient steamClient;
        private CallbackManager callbackManager;
        private SteamUser steamUser;
        private static WebAPI _instance;
        public static WebAPI Instance
        {
            get
            {
                return _instance ?? (_instance = new WebAPI());
            }
        }
        private WebAPI()
        {
            steamClient = new SteamClient();
            callbackManager = new CallbackManager(steamClient);

            IsRunning = true;
        }
        public async Task<bool> OnLogonAsync(string user = "anonymous", string pwd = "")
        {
            string _msg = "";
            _msg = $"OnLogonAsync";
            Log.Info($"{_msg}");


            if (user == null)
            {
                _msg = $"User can't be null";
                Log.Info($"{_msg}");
                throw new ArgumentException($"{_msg}");
            }
            if (user != "anonymous" && pwd != "")
            {
                _msg = $"Password can't be null if User is other than 'anonymous' ";
                Log.Info($"{_msg}");
                throw new ArgumentException($"{_msg}");
            }

            Username = user;
            passwd = pwd;

            _msg = $"{Username} Loging Start";
            Log.Info($"{_msg}");

            logonTaskCompletionSource = new TaskCompletionSource<bool>();
            steamUser = steamClient.GetHandler<SteamUser>();
            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnectedCallBack);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnectedCallBack);
            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLogOnCallBack);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLogOffCallBack);

            steamClient.Connect();
            Log.Info("Connected to Steam . . .");
            await logonTaskCompletionSource.Task;
            return logonTaskCompletionSource.Task.Result;

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="workshopIds"></param>
        /// <returns></returns>
        /// 


        public async Task<bool> DownloadPublishFileAsync(string url, string patch, string name)
        {
            var download = Task.Run(async delegate
            {
                using (var Client = new WebClient())
                {
                   /* var downloadTask =*/
                    await Client.DownloadFileTaskAsync(url, Path.Combine(patch, name));
                }

            });
            await Task.WhenAny(download, Task.Delay(100000));
            Log.Info($"Task Status {download.IsCompleted} ");

            if (MyZipFileProvider.IsZipFile(Path.Combine(patch, name)))
            {
                string Code = null;

                foreach (string item in MyFileSystem.GetFiles(Path.Combine(patch, name), ".cs", MySearchOption.AllDirectories))
                {
                    if (MyFileSystem.FileExists(item))
                    {
                        using (Stream stream = MyFileSystem.OpenRead(item))
                        {
                            using (StreamReader streamReader = new StreamReader(stream))
                            {
                                Code = streamReader.ReadToEnd();
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(Code))
                {
                    Log.Error("Error with Reading Code");
                    return false;
                }
                else
                {

                    string _path = Path.Combine(patch, name);  //$"{MyPlug.DownloadPatchCMD}\\{target.WorkshopID}";
                    //Log.Info(_path);
                    if (MyFileSystem.FileExists(_path))
                    {
                        File.Delete(_path);
                    }

                    Directory.CreateDirectory(_path);
                    File.WriteAllText(Path.Combine(_path, $"Script.cs"), Code);
                    return true;
                }


            }
            else
            {
                return false;
            }


        }

        //public async Task<bool> OnMergedDonloadedScriptAsync(string patch, string name)
        //{


        //    // if (MyFileSystem.FileExists(Path.Combine(patch, name)))
        //    //{
        //    //    foreach (string item in MyFileSystem.GetFiles(Path.Combine(patch, name), ".cs", MySearchOption.AllDirectories))
        //    //    {
        //    //        if (MyFileSystem.FileExists(item))
        //    //        {
        //    //            using (Stream stream = MyFileSystem.OpenRead(item))
        //    //            {
        //    //                using (StreamReader streamReader = new StreamReader(stream))
        //    //                {
        //    //                    Code = streamReader.ReadToEnd();
        //    //                }
        //    //            }
        //    //        }

        //    //    }
        //    //    if (string.IsNullOrEmpty(Code))
        //    //    {

        //    //        return false;
        //    //    }
        //    //    else
        //    //    {
        //    //         //Log.Info($"{Code}");
        //    //         //Directory.CreateDirectory()
        //    //         string _path = Path.Combine(patch, name);  //$"{MyPlug.DownloadPatchCMD}\\{target.WorkshopID}";
        //    //        Log.Info(_path);
        //    //        if (File.Exists(_path))
        //    //        {
        //    //            File.Delete(_path);
        //    //        }

        //    //        Directory.CreateDirectory(_path);
        //    //        File.WriteAllText(Path.Combine(_path, $"Script.cs"), Code);
        //    //        return true;
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    return false;
        //    //}

        //}

        public async Task<Dictionary<ulong, PublishedItemDetails>> GetPublishedFileDetailsAsync(IEnumerable<ulong> workshopIds)
        {
            //if (!IsReady)
            //    throw new Exception("ISMP_WorshopService not initialized!");
            using (dynamic remoteStorage = SteamKit2.WebAPI.GetInterface("ISteamRemoteStorage"))
            {
                KeyValue allFilesDetails = null;
                remoteStorage.Timeout = TimeSpan.FromSeconds(30);
                allFilesDetails = await Task.Run(delegate
                {
                    try
                    {
                        return remoteStorage.GetPublishedFileDetails1(
                            itemcount: workshopIds.Count(),
                            publishedfileids: workshopIds,

                            method: HttpMethod.Post);
                    }
                    catch (HttpRequestException e)
                    {
                        Log.Error($"Fetching File Details failed: {e.Message}");
                        return null;
                    }
                });
                if (allFilesDetails == null)
                    return null;
                //fileDetails = remoteStorage.Call(HttpMethod.Post, "GetPublishedFileDetails", 1, new Dictionary<string, string>() { { "itemcount", workshopIds.Count().ToString() }, { "publishedfileids", workshopIds.ToString() } });
                var detailsList = allFilesDetails?.Children.Find((KeyValue kv) => kv.Name == "publishedfiledetails")?.Children;
                var resultCount = allFilesDetails?.GetValueOrDefault<int>("resultcount");
                if (detailsList == null || resultCount == null)
                {
                    Log.Error("Received invalid data: ");
                    if (allFilesDetails != null)
                        PrintKeyValue(allFilesDetails);
                    return null;
                }
                if (detailsList.Count != workshopIds.Count() || resultCount != workshopIds.Count())
                {
                    Log.Error($"Received unexpected number of fileDetails. Expected: {workshopIds.Count()}, Received: {resultCount}");
                    return null;
                }

                var result = new Dictionary<ulong, PublishedItemDetails>();
                for (int i = 0; i < resultCount; i++)
                {
                    var fileDetails = detailsList[i];

                    var tagContainer = fileDetails.Children.Find(item => item.Name == "tags");
                    List<string> tags = new List<string>();
                    if (tagContainer != null)
                        foreach (var tagKv in tagContainer.Children)
                        {
                            var tag = tagKv.Children.Find(item => item.Name == "tag")?.Value;
                            if (tag != null)
                                tags.Add(tag);
                        }

                    result[workshopIds.ElementAt(i)] = new PublishedItemDetails()
                    {
                        PublishedFileId = fileDetails.GetValueOrDefault<ulong>("publishedfileid"),
                        Views = fileDetails.GetValueOrDefault<uint>("views"),
                        Subscriptions = fileDetails.GetValueOrDefault<uint>("subscriptions"),
                        TimeUpdated = Util.DateTimeFromUnixTimeStamp(fileDetails.GetValueOrDefault<ulong>("time_updated")),
                        TimeCreated = Util.DateTimeFromUnixTimeStamp(fileDetails.GetValueOrDefault<ulong>("time_created")),
                        Description = fileDetails.GetValueOrDefault<string>("description"),
                        Title = fileDetails.GetValueOrDefault<string>("title"),
                        FileUrl = fileDetails.GetValueOrDefault<string>("file_url"),
                        FileSize = fileDetails.GetValueOrDefault<long>("file_size"),
                        FileName = fileDetails.GetValueOrDefault<string>("filename"),
                        ConsumerAppId = fileDetails.GetValueOrDefault<ulong>("consumer_app_id"),
                        CreatorAppId = fileDetails.GetValueOrDefault<ulong>("creator_app_id"),
                        Creator = fileDetails.GetValueOrDefault<ulong>("creator"),
                        Tags = tags.ToArray()
                    };
                }
                return result;
            }
        }
        class Printable
        {
            public KeyValue Data;
            public int Offset;

            public void Print()
            {
                Log.Info($"{new string(' ', Offset)}{Data.Name}: {Data.Value}");
            }
        }
        private static void PrintKeyValue(KeyValue data)
        {

            var dataSet = new Stack<Printable>();
            dataSet.Push(new Printable()
            {
                Data = data,
                Offset = 0
            });
            while (dataSet.Count != 0)
            {
                var printable = dataSet.Pop();
                foreach (var child in printable.Data.Children)
                    dataSet.Push(new Printable()
                    {
                        Data = child,
                        Offset = printable.Offset + 2
                    });
                printable.Print();
            }
        }
        public void CancelLogon()
        {
            logonTaskCompletionSource?.SetCanceled();
        }


        #region CALLBACKS
        private void OnConnectedCallBack(SteamClient.ConnectedCallback callback)
        {
            Log.Info($"Connectd to SteamAPI! Loging {Username}'...'");
            if (Username == "anonymous")
                steamUser.LogOnAnonymous();
            else
                steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = Username,
                    Password = passwd
                });
        }
        private void OnDisconnectedCallBack(SteamClient.DisconnectedCallback callback)
        {
            Log.Info($"{Username} Disconnected");
            IsReady = false;
            IsRunning = false;
        }
        private void OnLogOnCallBack(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                string msg;
                if (callback.Result == EResult.AccountLogonDenied)
                {
                    msg = "Unable to logon to Steam: This account is Steamguard protected.";
                    Log.Warn(msg);
                    logonTaskCompletionSource.SetException(new Exception(msg));
                    IsRunning = false;
                    return;
                }

                msg = $"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}";
                Log.Warn(msg);
                logonTaskCompletionSource.SetException(new Exception(msg));
                IsRunning = false;
                return;
            }
            IsReady = true;
            Log.Info("Successfully logged on!");
            logonTaskCompletionSource.SetResult(true);
        }
        private void OnLogOffCallBack(SteamUser.LoggedOffCallback callback)
        {
            IsReady = false;
            Log.Info($"Logged off of Steam: {callback.Result}");
        }
        #endregion
    }

}
