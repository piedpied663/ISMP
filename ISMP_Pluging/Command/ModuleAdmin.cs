using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.FileSystem;
using MyPlug = ISMP_Pluging.BasePluging;
using MyConf = ISMP_Pluging.Conf.MyConfig;
using ISMP_Pluging.Class;
using ISMP_Pluging.ViewChildren.Util;
using ISMP_WorkshopService.Content;
using NLog;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;

namespace ISMP_Pluging.Command
{
    public class ModuleAdmin : CommandModule
    {
        private static readonly Logger Log = LogManager.GetLogger("@ISMP_Pluging ModuleAdmin");


        [Command("dll", "Download a script BY THE SERVER ITSELF ! Only Owner And Admin")]
        [Permission(MyPromoteLevel.Owner | MyPromoteLevel.Admin)]
        public async Task Dll(string _worshopID)
        {
            //if (!ulong.TryParse(_worshopID, out ulong workshopId))
            //{
            //    Context.Respond($"Invalid Request '{workshopId}' not found or ID is invalid.");
            //    return;
            //}
            //else
            //{

            //}
            ulong workshopId = await MyPlug.Instance.TryParseAsync(_worshopID);

            if (workshopId.ToString() == "0")
            {
                Context.Respond("Invalid ID");
                return;

            }
            else
            {

                PublishedItemDetails publishedItemDetails = null;
                var taskGetInfoFromGame = Task.Run(async delegate
                {
                    publishedItemDetails = await MyPlug.Instance.GetPublishedItemDetailsAsync(workshopId);
                });
                await Task.WhenAll(taskGetInfoFromGame);

                if (await MyPlug.Instance.IsAnIngameScriptAsync(publishedItemDetails.Tags))
                {
                    Context.Respond("Error with Download Its not an ingame Script");
                    return;
                }
                else
                {

                    if (await MyPlug.Instance.HasAnUrlAsync(publishedItemDetails.FileUrl))
                    {
                        if (!await MyPlug.Instance.DownloadBySteamWoshopServiceAsync(publishedItemDetails.FileUrl, $"{workshopId}"))
                        {
                            Context.Respond("Error with Download ");
                            return;
                        }
                        else
                        {
                            Context.Respond($"Succefully download {publishedItemDetails.Title} ID > {workshopId}");
                            await Task.WhenAll(MyPlug.Instance.Addtolist(publishedItemDetails, true));
                            return;
                        }
                    }
                    else
                    {
                        if (!await MyPlug.Instance.BypassBySteamCMDAsync(workshopId))
                        {
                            Context.Respond("Error with Download using Bypass");
                            return;
                        }
                        else
                        {
                            Context.Respond($"Succufully Downloaded {publishedItemDetails.Title}");
                            await Task.WhenAll(MyPlug.Instance.Addtolist(publishedItemDetails, true));
                            return;                          
                        }
                    }
                }

            }

            //if (workshopId != 0)
            //{

            //    if (!MyFileSystem.DirectoryExists($"{$"{MyPlug.DownloadPatchCMD}" + $"{workshopId}"}"))
            //    {
            //        var script = new MyRequestScript
            //        {
            //            WorkShopID = workshopId
            //        };

            //        PublishedItemDetails publishedItemDetails = null;

            //        var taskGetInfo = Task.Run(async delegate
            //        {
            //            publishedItemDetails = (await script.GetPublishedItemDetailsAsync());

            //            if (publishedItemDetails == null)
            //            {
            //                string statusMsg = $"No Details Found";
            //                Log.Error($"{statusMsg}");
            //            }
            //            Log.Info($"TaskGetInfo >  {publishedItemDetails.Title}");

            //        });
            //        await Task.WhenAny(taskGetInfo, Task.Delay(15000));
            //        if (publishedItemDetails != null)
            //        {
            //            foreach (string tag in publishedItemDetails.Tags)
            //            {
            //                if (!tag.Contains("ingameScript"))
            //                {

            //                    Context.Respond($"Request '{workshopId}' Not an Ingame Script");
            //                    return;
            //                }
            //            }
            //        }


            //        if (!string.IsNullOrEmpty(publishedItemDetails.FileUrl))//URL ADDED BY HER CREATOR !
            //        {

            //            if (!await script.DownloadBySteamWoshopServiceAsync(publishedItemDetails.FileUrl, workshopId.ToString()))
            //            {
            //                string statusMsg = "Download Error by Worshopservice";
            //                Log.Error($"{statusMsg}");
            //                Context.Respond($"{statusMsg}.");

            //            }
            //            else
            //            {
            //                Context.Respond($"Found < {publishedItemDetails.CreatorAppId} > Founded < {publishedItemDetails.Title} > . Starting Download");
            //                string statusMsg = $"Download {publishedItemDetails.Title} suceffuly end";
            //                Log.Info($"{statusMsg}");
            //                Context.Respond($"{statusMsg}.");
            //                await MyPlug.Instance.AddtoListFromGame(publishedItemDetails);
            //            }
            //        }
            //        else
            //        {
            //            string statusMsg = "No Url Added Error by Worshopservice BYPASS STARTING !";
            //            if (!await script.BypassBySteamCMDAsync())
            //            {

            //                statusMsg = $"Download {publishedItemDetails.Title} Error Can't Download";

            //                Log.Error($"{statusMsg}");
            //                Context.Respond($"{statusMsg}.");
            //            }
            //            else
            //            {
            //                await MyPlug.Instance.AddtoListFromGame(publishedItemDetails);
            //                Context.Respond($"Succefully Download.");

            //            }

            //        }
            //    }
            //    else
            //    {
            //        Context.Respond($"Invalid Request '{workshopId}' Already found In DownloadFolder.");
            //        return;
            //    }
            //}
            //else
            //{
            //    Context.Respond($"Ivalid Request Not an Valid ID");
            //    return;
            //}




        }////ENDTASK


        [Command("whlist", "Enable [!whlist 00000000 true] or Disable [!whlist 00000000 false] in Whithelist a script after download It with dll ")]
        [Permission(MyPromoteLevel.Owner | MyPromoteLevel.Admin)]
        public async Task Whlist(string _worshoID, bool action)
        {
            var interact = await MyPlug.Instance.InteractWithScriptFromGameAsync(_worshoID, action);

            if (!interact)
            {
                Context.Respond("Error with requested script");

                return;
            }
            else
            {

                Context.Respond("Succefully Ended !");
                return;
            }
        }

        [Command("whlistdel", "Delete a script fromServer [!whlistdel 00000000] The Script must be Disabled By WhitheList !")]
        [Permission(MyPromoteLevel.Owner | MyPromoteLevel.Admin)]
        public async Task Whlistdel(string _worshopID)
        {
            var delete = await MyPlug.Instance.RemoveScriptFromGameAsync(_worshopID);
            if (!delete)
            {
                Context.Respond("Error with request Remove");
                return;
            }
            else
            {
                Context.Respond("Successfully Removed script from server");
                return;
            }
        }

    }
}
