using ChameHOT_Service.Models;
using ChameHOT_Service.Resources;
using NoteOne_Core;
using NoteOne_Core.Command;
using NoteOne_Core.Common;
using NoteOne_Core.UI.Common;
using NoteOne_ImageHelper;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using NoteOne_Utility.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Imaging;

namespace ChameHOT_Service.UI.ViewModels
{
    public class ChameHOTSettingViewModel : ViewModelBase
    {
        private static bool isAddedSettingPanes;
        private readonly ChameHOTBackgroundTaskService chameHOTBackgroundTaskService;
        private readonly ChameHOTUpdateTileBackgroundTaskService chameHOTUpdateTileBackgroundTaskService;
        private readonly ChameHOTQueryService chameHOTQueryService;
        private readonly ChameHOTServiceChannel chameHOTServiceChannel;

        // Used for pointer move, offset of pointer in element
        private double offsetX, offsetY;

        public ChameHOTSettingViewModel(FrameworkElement view, Dictionary<string, object> pageState) :
            base(view, pageState)
        {
            chameHOTServiceChannel = ServiceChannelManager.CurrentServiceChannelManager["CHOTSC"] as ChameHOTServiceChannel;
            chameHOTQueryService = chameHOTServiceChannel["CHOTQS"] as ChameHOTQueryService;
            if (chameHOTQueryService.IsSupportBackgroundTask)
                chameHOTBackgroundTaskService = chameHOTQueryService.BackgroundTaskService as ChameHOTBackgroundTaskService;

            var chameHOTQueryServiceUpdateTile = chameHOTServiceChannel["CHOTQSUT"] as ChameHOTQueryService;
            if (chameHOTQueryServiceUpdateTile.IsSupportBackgroundTask)
                chameHOTUpdateTileBackgroundTaskService = chameHOTQueryServiceUpdateTile.BackgroundTaskService as ChameHOTUpdateTileBackgroundTaskService;

            if (chameHOTBackgroundTaskService != null)
            {
                this["BackgroundTaskTimeTiggerTimes"] = chameHOTBackgroundTaskService.TimeTriggerTimes;
                this["BackgroundTaskTimeTiggerTime"] = "15";
            }

            // setting for setup
            this["Settings"] = ChameHOTServiceSetting.Instance.Settings;

            this["IsEditing"] = false;
            this["IsAppearanceShown"] = false;
            this["IsBackgroundPopupShown"] = false;

            this["HistoryOnToday"] = null;

            // Layout properties
            this["ShowInLeft"] = SettingPanelPosition((double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_LEFT]);

            #region Commands

            this["LoadHotCommand"] = new RelayCommand(async () => { await LoadContent(); });

            #region Navigate to WikiPedia to see more command

            this["SeeMoreCommand"] = new RelayCommand(async () =>
            {
                try
                {
                    var hot = this["HistoryOnToday"] as HistoryOnToday;
                    if (hot != null)
                    {
                        var uri = new Uri(hot.Link);
                        await Launcher.LaunchUriAsync(uri);
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                }
            });

            #endregion

            #region ShowAppearanceCommand

            this["ShowAppearanceCommand"] = new RelayCommand<Windows.UI.Xaml.Controls.Primitives.Popup>(p =>
            {
                var showInLeft = (bool)this["ShowInLeft"];
                this["IsAppearanceShown"] = true;
                if (showInLeft)
                {
                    p.HorizontalOffset = 340;
                }
                else
                {
                    p.HorizontalOffset = Window.Current.Bounds.Width - 690;
                }
            });

            #endregion

            #region Set current image to lockscreen

            this["SetLockScreenCommand"] = new RelayCommand(async () =>
            {
                try
                {
                    var hot = this["HistoryOnToday"] as HistoryOnToday;
                    var writeableBitmap = this["PreviewImage"] as WriteableBitmap;

                    if (hot != null && writeableBitmap != null)
                    {
                        var image = await ChameHOTServiceHelper.GenerateLockScreenAsync(hot, writeableBitmap);

                        // Set new lockscreen
                        await LockScreen.SetImageStreamAsync(await image.ToRandomAccessStreamAsync());

                        // Update cache keys, in MD5
                        await ChameHOTCacheKeys.AddKey(MD5Encryptor.GetMD5(await ChameHOTServiceHelper.GetCurrentLockScreen()));

                        new MessagePopup(ResourcesLoader.Loader["SetLockScreenSucessfully"]).Show();

                        await LoadContent();
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                    new MessagePopup(ResourcesLoader.Loader["SetLockScreenError"]).Show();
                }
            });

            #endregion

            #region RegisterBackgroundTaskCommand

            this["ShowBackgroundTaskPopupCommand"] = new RelayCommand<Windows.UI.Xaml.Controls.Primitives.Popup>(p =>
            {
                var showInLeft = (bool)this["ShowInLeft"];
                this["IsBackgroundPopupShown"] = true;
                if (showInLeft)
                {
                    p.HorizontalOffset = 340;
                }
                else
                {
                    p.HorizontalOffset = Window.Current.Bounds.Width - 690;
                }
            });

            this["ClosePopupCommand"] = new RelayCommand(() =>
            {
                this["IsBackgroundPopupShown"] = false;
            });

            this["RegisterBackgroundTaskCommand"] = new RelayCommand(() =>
            {
                try
                {
                    if (chameHOTBackgroundTaskService != null)
                        chameHOTBackgroundTaskService.InitializeBackgroundTask(
                            new TimeTrigger(this["BackgroundTaskTimeTiggerTime"].ToString().StringToUInt(), false),
                            null);

                    new MessagePopup(ResourcesLoader.Loader["SetBackgroundTaskSucessfully"]).Show();
                    this["IsBackgroundPopupShown"] = false;
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                }
            });

            #endregion

            #region UnregisterBackgroundTaskCommand

            this["UnregisterBackgroundTaskCommand"] = new RelayCommand(() =>
            {
                try
                {
                    if (chameHOTBackgroundTaskService != null)
                    {
                        chameHOTBackgroundTaskService.UnregisterBackgroundTask();
                        new MessagePopup(ResourcesLoader.Loader["UnregisterBackgroundTaskSucessfully"]).Show();
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteLog();
                }
            });

            #endregion

            #region Show About Command

            this["ShowInfoCommand"] = new RelayCommand(() => ShowAboutPane(SettingSourceFlag.ByApp));

            #endregion

            #region ShowHelpCommand

            this["ShowHelpCommand"] = new RelayCommand(async () =>
            {
                var uri = new Uri(ResourcesLoader.Loader["HelpPageUrl"]);
                await Launcher.LaunchUriAsync(uri);
            });

            #endregion

            #region SaveSettingsCommand
            this["SaveSettingsCommand"] = new RelayCommand(async () => await NoteOne_Utility.AppSettings.SaveSettings(ChameHOTServiceSetting.Instance));
            #endregion

            #region Pointer Move Event Commands

            this["PointerPressedCommand"] = new RelayCommand<object[]>(p =>
            {
                this["IsEditing"] = true;
                var args = p[1] as PointerRoutedEventArgs;
                var element = p[0] as FrameworkElement;
                if (args != null && element != null)
                {
                    var currentPosition = args.GetCurrentPoint(element).Position;
                    offsetX = element.ActualWidth / 2 - currentPosition.X + 5;
                    offsetY = element.ActualHeight / 2 - currentPosition.Y + 5;
                }
            });

            this["PointerReleasedCommand"] = new RelayCommand<object[]>(async p =>
            {
                this["IsEditing"] = false;
                await NoteOne_Utility.AppSettings.SaveSettings(ChameHOTServiceSetting.Instance);
            });

            this["PointerMovedCommand"] = new RelayCommand<object[]>(p =>
            {
                if (!(bool)this["IsEditing"]) return;

                var args = p[1] as PointerRoutedEventArgs;
                var panel = p[0] as UIElement;
                var left = (double)(ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_LEFT]);
                var top = (double)(ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_TOP]);

                if (args != null && panel != null)
                {
                    Point position = args.GetCurrentPoint(panel).Position;

                    // check current drag position and set the setting panel position on LEFT or RIGHT
                    this["ShowInLeft"] = SettingPanelPosition(position.X);

                    ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_LEFT] = position.X + offsetX;
                    ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_TOP] = position.Y + offsetY;
                }
            });

            #endregion

            #endregion

            #region Register Charm Setting Panel

            if (!isAddedSettingPanes)
            {
                SettingsPane.GetForCurrentView().CommandsRequested += RegisterSettingPanes;
                isAddedSettingPanes = true;
            }

            #endregion

            // First time to load lockbackground and content
            LoadContent();

            // Register backgroundtask for updating tile
            RegisterUpdatingTileBackgroundTask();
        }

        /// <summary>
        /// Load contents for current view
        /// </summary>
        /// <returns></returns>
        private async Task LoadContent()
        {
            // Try to read data from local cache
            var hotCache = await chameHOTQueryService.QueryDataCacheAsync();
            this["HistoryOnToday"] = hotCache;

            // Set display time and date
            this["TimeNow"] = DateTime.Now.ToString("H:mm");
            this["DateToday"] = string.Format("{0:M}, {0:dddd}", DateTime.Now, CultureInfo.CurrentCulture);

            var bitmapImage = await ChameHOTServiceHelper.InitBitmapFromLockScreenAsync();
            this["PreviewImage"] = bitmapImage;

            var hot = await chameHOTQueryService.QueryDataAsync();
            if (hot != null)
                this["HistoryOnToday"] = hot;

            // Update Tile
            ChameHOTServiceHelper.UpdateTile(hot ?? hotCache);
        }

        // Register backgroundtask for updating tile
        private async Task RegisterUpdatingTileBackgroundTask()
        {
            try
            {
                if (chameHOTUpdateTileBackgroundTaskService != null)
                    chameHOTUpdateTileBackgroundTaskService.InitializeBackgroundTask(
                        new TimeTrigger(chameHOTUpdateTileBackgroundTaskService.TimeTriggerTime, false),
                        null);
            }
            catch (Exception ex)
            {
                ex.WriteLog();
            }
        }

        private void RegisterSettingPanes(SettingsPane settingsPane, SettingsPaneCommandsRequestedEventArgs e)
        {
            // Show help guideline
            var helpCommand = new SettingsCommand("helpPage", ResourcesLoader.Loader["Help"], async command =>
            {
                var uri = new Uri(ResourcesLoader.Loader["HelpPageUrl"]);
                await Launcher.LaunchUriAsync(uri);
            });
            e.Request.ApplicationCommands.Add(helpCommand);

            // For Privacy Policy
            var privacyPolicyCommand =
                new SettingsCommand("privacyPolicyPage", ResourcesLoader.Loader["PrivacyPolicy"], command => ShowAboutPane(SettingSourceFlag.ByCharm));
            e.Request.ApplicationCommands.Add(privacyPolicyCommand);

            var aboutCommand = new SettingsCommand("aboutPage", ResourcesLoader.Loader["About"],
                                                   command => ShowAboutPane(SettingSourceFlag.ByCharm));
            e.Request.ApplicationCommands.Add(aboutCommand);
        }

        private void ShowAboutPane(SettingSourceFlag flag)
        {
            var information = XamlReader.Load(ResourcesLoader.Loader["AboutContent"]) as UIElement;
            if (information != null)
            {
                var settingPopup = new SettingPopup(ResourcesLoader.Loader["AboutTitle"], flag)
                {
                    Content = information
                };
                settingPopup.Show();
            }
        }

        private bool SettingPanelPosition(double position)
        {
            if (position < 400)
            {
                return false;
            }

            if (position + 400 > (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_WIDTH] - 400)
            {
                return true;
            }

            return true;
        }

        public override void LoadState()
        {
        }

        public override void SaveState(Dictionary<string, object> pageState)
        {
        }
    }
}
