using ChameHOT.BackgroundTask;
using ChameHOT_Service.Models;
using ChameHOT_Service.Resources;
using Newtonsoft.Json;
using NoteOne_Core;
using NoteOne_ImageHelper;
using NoteOne_Utility;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using NoteOne_Utility.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.System.UserProfile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using NoteOne_ImageHelper.ImageSecurity;
using System.IO;
using Windows.Storage;

namespace ChameHOT_Service
{
    public class ChameHOTBackgroundTaskService : BackgroundTaskService
    {
        private static readonly AsyncLock CACHE_FILE_WRITE_ASYNC_LOCKER = new AsyncLock();

        public ChameHOTBackgroundTaskService(Service service, XmlElement configXml)
            : base(service, configXml, UpdateLockScreenBackgroundTask.BackgroundTaskSettingFileName)
        {
            DoAsync = Task_Run;
        }

        #region Properties

        public List<object> TimeTriggerTimes { get; private set; }

        #endregion

        protected override void Initialize(XmlElement configXml)
        {
            base.Initialize(configXml);

            TimeTriggerTimes = new List<object>();

            try
            {
                string[] times = configXml.GetAttribute("TimeTriggerTimes").Check("").StringToArray();
                foreach (string time in times)
                {
                    string[] tc = time.StringToArray(':');
                    if (tc.Length > 1)
                        TimeTriggerTimes.Add(new { Name = ResourcesLoader.Loader[tc[0]], Value = tc[1] });
                }
            }
            catch (Exception ex)
            {
                ex.WriteLog();
            }
        }

        private async Task Task_Run(Dictionary<string, string> parameters)
        {
            try
            {
                // Initialize and read app local&roaming setting
                await AppSettings.InitializeSettings(AppSettings.Instance);
                await AppSettings.InitializeSettings(ChameHOTServiceSetting.Instance);
                await AppSettings.InitializeSettings(ChameHOTRoamingSetting.Instance);

                var chameHOTQueryService = Service as ChameHOTQueryService;
                if (chameHOTQueryService != null)
                {
                    var cacheFolder = ApplicationData.Current.LocalFolder;
                    var hot = await chameHOTQueryService.QueryDataAsync();
                    var clientId = (string)ChameHOTRoamingSetting.Instance.Settings[ChameHOTRoamingSetting.CLIENT_ID];
                    if (hot != null)
                    {
                        byte[] imageBytes = await ChameHOTServiceHelper.GetCurrentLockScreen();
                        var key = MD5Encryptor.GetMD5(imageBytes);

                        // Check and get local cache image
                        if (!await ChameHOTCacheKeys.IsContainKey(key))
                        {
                            using (AsyncLock.Releaser releaser = await CACHE_FILE_WRITE_ASYNC_LOCKER.LockAsync())
                            {
                                // Save current image into cache folder
                                var cahcheImg = await cacheFolder.CreateFileAsync(clientId, CreationCollisionOption.OpenIfExists);
                                using (var ras = await cahcheImg.OpenAsync(FileAccessMode.ReadWrite))
                                {
                                    await ras.WriteAsync(imageBytes.AsBuffer());
                                }
                            }
                        }
                        else
                        {
                            using (AsyncLock.Releaser releaser = await CACHE_FILE_WRITE_ASYNC_LOCKER.LockAsync())
                            {
                                if (await cacheFolder.CheckFileExisted(clientId))
                                {
                                    var buffer = await FileIO.ReadBufferAsync(await cacheFolder.GetFileAsync(clientId));
                                    imageBytes = buffer.ToArray();
                                }
                            }
                        }

                        // Wrap http request for remote request lockscreen
                        var request = new BackgroundTaskRequestModel()
                        {
                            ClientId = (string)ChameHOTRoamingSetting.Instance.Settings[ChameHOTRoamingSetting.CLIENT_ID],
                            HOT = hot,
                            Position = new Point((double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_LEFT],
                                                 (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_TOP]),
                            ScreenSize = new Size((double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_WIDTH],
                                                  (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_HEIGHT]),
                            ImageBase64 = Convert.ToBase64String(imageBytes),
                            HasBackground = (bool)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.BACKCOLOR_ON],
                            BackgroundColor = (string)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.BACKCOLOR]
                        };

                        var requestJson = JsonConvert.SerializeObject(request);
                        var imageResponse = await HttpClientHelper.Instance.PostResponseStringAsync(new Uri(chameHOTQueryService.ChameServiceUrl), requestJson);

                        // Set new lockscreen from remote ChameService
                        using (var ms = new MemoryStream(Convert.FromBase64String(imageResponse.Body)))
                        {
                            await LockScreen.SetImageStreamAsync(ms.AsRandomAccessStream());
                        }

                        // Update cache keys
                        await ChameHOTCacheKeys.AddKey(MD5Encryptor.GetMD5(await ChameHOTServiceHelper.GetCurrentLockScreen()));
                    }
                }
            }
            catch (Exception ex)
            {
                ex.WriteLog();
            }
        }
    }
}
