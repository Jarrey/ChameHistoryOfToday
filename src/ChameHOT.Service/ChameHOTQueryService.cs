using NoteOne_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using ChameHOT_Service.Models;
using Windows.System.UserProfile;
using Windows.Globalization;
using Windows.Storage;
using Newtonsoft.Json;
using NoteOne_Utility.Helpers;

namespace ChameHOT_Service
{
    /// <summary>
    ///     ID is [Guid("799BBBE6-0C61-40DA-94FF-165470D1D80A")]
    ///     and [Guid("B3F0DA47-F4B8-49EA-974E-91955FDC8E06")]
    /// </summary>
    public class ChameHOTQueryService : Service
    {
        #region Fields

        private const string HotCacheFile = "hot_cache.json";

        private static readonly AsyncLock DATA_FILE_WRITE_ASYNC_LOCKER = new AsyncLock();

        #endregion

        public ChameHOTQueryService(ServiceChannel serviceChannel, XmlElement configXml) :
            base(serviceChannel, configXml)
        {
        }

        #region Properties

        public string[] Regions { get; private set; }
        public string CurrentRegion { get; private set; }

        public string ChameServiceUrl { get; private set; }

        #endregion

        protected override void InitializeService(XmlElement configXml)
        {
            base.InitializeService(configXml);

            if (ID.CompareTo(new Guid("799BBBE6-0C61-40DA-94FF-165470D1D80A")) != 0 &&
                ID.CompareTo(new Guid("B3F0DA47-F4B8-49EA-974E-91955FDC8E06")) != 0)
                throw new InvalidOperationException("The Service ID is incorrect.");

            try
            {
                ChameServiceUrl = configXml.GetAttribute("ChameServiceUrl").Check("");

                Regions = configXml.GetAttribute("Regions").Check().StringToArray();
                // Set the current region
                var language = new Language(GlobalizationPreferences.Languages[0]);
                string currentLanaguageTag = language.LanguageTag.Substring(0, 2);
                CurrentRegion = Regions[0];
                foreach (string r in Regions)
                {
                    if (r.ToUpperInvariant().Contains(currentLanaguageTag.ToUpperInvariant()))
                    {
                        CurrentRegion = r;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.WriteLog();
            }
        }

        protected override void InitializeParameters(object[] parameters)
        {
            try
            {
                base.InitializeParameters(parameters);
                (ServiceApiParameters as ChameHOTApiParameter)
                    .Region = (string)parameters[0];
            }
            catch (Exception ex)
            {
                ex.WriteLog();
                throw ex;
            }
        }

        #region Async Query API

        public IAsyncOperation<HistoryOnToday> QueryDataAsync()
        {
            return AsyncInfo.Run(async (token) =>
            {
                try
                {
                    if (!CheckNetworkStatus()) return null;

                    HistoryOnToday hot = null;
                    InitializeParameters(new object[] { CurrentRegion });
                    object queryResult = await QueryDataAsyncInternal();
                    if (null != queryResult)
                        hot = (new ChameHOTQueryResult(queryResult, CurrentRegion)).Result as HistoryOnToday;

                    // save current hot query result to file cache
                    using (AsyncLock.Releaser releaser = await DATA_FILE_WRITE_ASYNC_LOCKER.LockAsync())
                    {
                        StorageFile file = null;
                        if (!await ApplicationData.Current.LocalFolder.CheckFileExisted(HotCacheFile))
                        {
                            file = await ApplicationData.Current.LocalFolder.CreateFileAsync(HotCacheFile, CreationCollisionOption.ReplaceExisting);
                        }
                        else
                        {
                            file = await ApplicationData.Current.LocalFolder.GetFileAsync(HotCacheFile);
                        }

                        await FileIO.WriteTextAsync(file, await JsonConvert.SerializeObjectAsync(hot));
                    }

                    return hot;
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                    return null;
                }
            });
        }

        public IAsyncOperation<HistoryOnToday> QueryDataCacheAsync()
        {
            return AsyncInfo.Run(async (token) =>
            {
                try
                {
                    using (AsyncLock.Releaser releaser = await DATA_FILE_WRITE_ASYNC_LOCKER.LockAsync())
                    {
                        if (await ApplicationData.Current.LocalFolder.CheckFileExisted(HotCacheFile))
                        {
                            return await JsonConvert.DeserializeObjectAsync<HistoryOnToday>(await FileIO.ReadTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync(HotCacheFile)));
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                    return null;
                }
            });
        }

        #endregion
    }
}
