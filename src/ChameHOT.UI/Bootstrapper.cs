using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Data.Xml.Dom;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Activation;
using NoteOne_Core.Common;
using NoteOne_Utility.Extensions;
using NoteOne_Utility.Converters;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Notifications;
using NoteOne_Core.Interfaces;
using Windows.UI.Popups;
using Windows.System;
using ChameHOT_Service.UI.Views;
using ChameHOT_Service;
using NoteOne_Utility;
using System.Runtime.Serialization;

namespace ChameHOT
{
    [DataContract]
    public class Bootstrapper
    {
        private Frame rootFrame;
        private LaunchActivatedEventArgs launchArgs;

        // [Guid("0D75C8B3-D9C8-4C94-8773-D034CAD8B5C2")] Alpha - 1.1
        // [Guid("5B4FDE1D-197D-4990-97A5-6D474FBEB775")] 1.3 - Update to use local cache
        // [Guid("2018F461-B3AD-452E-9214-6157BBC5C332")] 1.4 - Appearance
        private const string upgradeKey = "2018F461-B3AD-452E-9214-6157BBC5C332"; 

        private Bootstrapper() { }

        private static Bootstrapper _currentInstance;
        public static Bootstrapper CurrentBootstrapper
        {
            get
            {
                if (_currentInstance == null)
                    _currentInstance = new Bootstrapper();
                return _currentInstance;
            }
        }

        public void Run(LaunchActivatedEventArgs args)
        {
            launchArgs = args;
            if (Window.Current.Content == null)
            {
                rootFrame = new Frame();
                rootFrame.Navigate(typeof(Splash), args.SplashScreen);
                Window.Current.Content = rootFrame;
            }
            Window.Current.Activate();
        }

        public async Task Suspending()
        {
            try
            {
                await SuspensionManager.SaveAsync();
            }
            catch (SuspensionManagerException e)
            {
                e.WriteLog();
            }
            finally
            {
                NetworkStatusMonitor.CurrentNetworkStatusMonitor.UnRegisterForNetworkStatusChangeNotif();
                LogExtension.DestroyLogger();
            }
        }

        #region Initialize Data

        private IAsyncAction InitializeConfigAsync()
        {
            return AsyncInfo.Run(async token =>
            {
                try
                {
                    StorageFileQueryResult queryResult = Windows.ApplicationModel.Package.Current.InstalledLocation.
                        CreateFileQueryWithOptions(
                            new QueryOptions(CommonFileQuery.OrderByName, new List<string>() { ".mf" })
                            {
                                FolderDepth = FolderDepth.Deep
                            });


                    foreach (StorageFile file in await queryResult.GetFilesAsync())
                    {
                        // Load the service channels from config files
                        XmlDocument configXml = await XmlDocument.LoadFromFileAsync(file);
                        foreach (XmlElement s in configXml.GetElementsByTagName("ServiceChannel"))
                        {
                            Activator.CreateInstance(s.GetAttribute("Type").CheckAndThrow().GenerateType(), s);
                        }

                        // Load the known types
                        XmlNodeList knownTypes = configXml.GetElementsByTagName("KnownTypes");
                        if (knownTypes != null && knownTypes.Count > 0)
                        {
                            foreach (XmlElement assemblyElement in knownTypes.Item(0).SelectNodes("Type"))
                            {
                                Type knownType = assemblyElement.GetAttribute("Name").GenerateType();
                                if (!SuspensionManager.KnownTypes.Contains(knownType))
                                    SuspensionManager.KnownTypes.Add(knownType);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                }
            });
        }

        public IAsyncAction InitializeData()
        {
            return AsyncInfo.Run(async token =>
            {
                // Cleanup temp folder files
                foreach (IStorageItem item in await ApplicationData.Current.TemporaryFolder.GetItemsAsync())
                    await item.DeleteAsync();

                // Initialize aplication settings
                await AppSettings.InitializeSettings(AppSettings.Instance);
                await AppSettings.InitializeSettings(ChameHOTServiceSetting.Instance);

                // Initialize roaming aplication settings
                await AppSettings.InitializeSettings(ChameHOTRoamingSetting.Instance);

                // Check upgrade key to do cleanup after upgrading
                if (ChameHOTServiceSetting.Instance[ChameHOTServiceSetting.UPGRADE_KEY].ToString() != upgradeKey)
                {
                    await CleanUpAsync();
                    ChameHOTServiceSetting.Instance[ChameHOTServiceSetting.UPGRADE_KEY] = upgradeKey;
                    await AppSettings.SaveSettings(ChameHOTServiceSetting.Instance);
                }

                AppSettings.Instance.Settings[AppSettings.GLOBAL_NETWORK_TIMEOUT] = 120000; // 120s network timeout
                await AppSettings.SaveSettings(AppSettings.Instance);

                // Initialize log sub-system
                await LogExtension.InitializeLogger();

                // Initialize network monitor
                await NetworkStatusMonitor.CurrentNetworkStatusMonitor.CheckInternetStatusAsync();
                NetworkStatusMonitor.CurrentNetworkStatusMonitor.RegisterForNetworkStatusChangeNotif();

                // Read all *.mf config files to initialize all service channels and services
                await InitializeConfigAsync();

                // Complete data initialization
                this.IsDataInitialized = true;
            });
        }

        #endregion

        public async void LoadingCompleted()
        {
            await rootFrame.Dispatcher.RunAsync(CoreDispatcherPriority.High,
               async () =>
               {
                   if (launchArgs.PreviousExecutionState == ApplicationExecutionState.Running)
                   {
                       CoreApplication.Properties.Clear();
                       Window.Current.Activate();
                       return;
                   }

                   rootFrame = new Frame();
                   // App do not need to support suspending status
                   // SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                   if (launchArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                   {
                       // Restore the saved session state only when appropriate
                       try
                       {
                           await InitializeData();
                           await SuspensionManager.RestoreAsync();
                       }
                       catch (SuspensionManagerException ex)
                       {
                           ex.WriteLog();
                       }
                   }

                   if (rootFrame.Content == null)
                   {
                       // navigate to start page, ChameHOT.Service UI
                       if (!rootFrame.Navigate(typeof(ChameHOTSettingView), this)) 
                       {
                           new Exception("Failed to create initial page").WriteLog();
                       }
                   }
                   Window.Current.Content = rootFrame;
                   TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
               });
        }

        #region Cleanup Method

        /// <summary>
        /// Used for cleanup old data after app upgrading
        /// </summary>
        /// <returns></returns>
        private async Task CleanUpAsync()
        {
            // TODO: Implement custom cleanup logical here

            //foreach (StorageFile file in await ApplicationData.Current.LocalFolder.GetFilesAsync())
            //    await file.DeleteAsync();
            //foreach (StorageFile file in await ApplicationData.Current.RoamingFolder.GetFilesAsync())
            //    await file.DeleteAsync();

            //AppSettings.Instance.Reset();
            //ChameHOTServiceSetting.Instance.Reset();
            //ChameHOTRoamingSetting.Instance.Reset();

            // Initialize roaming aplication settings
            //await AppSettings.InitializeSettings(AppSettings.Instance);

            // Initialize aplication settings
            //await AppSettings.InitializeSettings(ChameHOTServiceSetting.Instance);
            //await AppSettings.InitializeSettings(ChameHOTRoamingSetting.Instance);

            // unregister all UpdateLockScreenBackgroundTask tasks from previous version
            //BackgroundTaskController.UnregisterBackgroundTasks("UpdateChameHOT_LSBackgroundTask");
        }

        #endregion

        #region Properties

        [DataMember]
        public bool IsDataInitialized { get; private set; }

        #endregion
    }
}
