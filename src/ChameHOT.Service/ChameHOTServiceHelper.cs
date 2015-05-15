using ChameHOT_Service.Models;
using ChameHOT_Service.Resources;
using NoteOne_Core.Notifications;
using NoteOne_Core.Notifications.TileContent;
using NoteOne_ImageHelper;
using NoteOne_Utility;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using NoteOne_Utility.Helpers;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;

namespace ChameHOT_Service
{
    internal class ChameHOTServiceHelper
    {
        private static readonly AsyncLock CACHE_FILE_WRITE_ASYNC_LOCKER = new AsyncLock();

        /// <summary>
        /// Initialize bitmap from current lockscreen
        /// </summary>
        /// <returns>WriteableBitmap</returns>
        public static async Task<WriteableBitmap> InitBitmapFromLockScreenAsync()
        {
            try
            {
                // Do image validation
                // Create image cache folder in local application data
                var cacheFolder = ApplicationData.Current.LocalFolder;

                var writeableBitmap = BitmapFactory.New(1, 1);
                byte[] lockscreen = null;
                using (var s = LockScreen.GetImageStream())
                {
                    lockscreen = new byte[s.Size];
                    await s.ReadAsync(lockscreen.AsBuffer(), (uint)s.Size, InputStreamOptions.None);
                    s.Seek(0);
                    writeableBitmap = await writeableBitmap.FromStream(s);
                }

                var clientId = (string)ChameHOTRoamingSetting.Instance.Settings[ChameHOTRoamingSetting.CLIENT_ID];
                var key = MD5Encryptor.GetMD5(lockscreen);
                if (!await ChameHOTCacheKeys.IsContainKey(key))
                {
                    using (AsyncLock.Releaser releaser = await CACHE_FILE_WRITE_ASYNC_LOCKER.LockAsync())
                    {
                        // Save current image into cache folder
                        var cahcheImg = await cacheFolder.CreateFileAsync(clientId, CreationCollisionOption.OpenIfExists);
                        using (var ras = await cahcheImg.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            await RandomAccessStream.CopyAsync(await writeableBitmap.ToRandomAccessStreamAsync(), ras);
                        }
                    }
                }
                else
                {
                    using (AsyncLock.Releaser releaser = await CACHE_FILE_WRITE_ASYNC_LOCKER.LockAsync())
                    {
                        if (await cacheFolder.CheckFileExisted(clientId))
                        {
                            var cacheImg = await cacheFolder.GetFileAsync(clientId);
                            writeableBitmap = await writeableBitmap.FromStream(await cacheImg.OpenAsync(FileAccessMode.ReadWrite));
                        }
                    }
                }

                return writeableBitmap;
            }
            catch (Exception ex)
            {
                ex.WriteLog();
                return null;
            }
        }

        /// <summary>
        /// Generate lockscreen image with on this day record.
        /// </summary>
        /// <param name="hot">The current content from network about OnThisDay</param>
        /// <param name="bitmap">The bitmap want to show.</param>
        /// <param name="ratio">Current image with display resolution ratio.</param>
        /// <returns>WriteableBitmap</returns>
        public static async Task<WriteableBitmap> GenerateLockScreenAsync(HistoryOnToday hot, WriteableBitmap bitmap)
        {
            try
            {
                // resize to screen size
                bitmap = await ResizeBitmap(bitmap);
                double rectWidth = 382;
                double bgWidth = 400;
                bool hasBackground = (bool)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.BACKCOLOR_ON];
                var backcolor = ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.BACKCOLOR].ToString().StringToColor();

                var rect = new Rect(9 + (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_LEFT],
                                    9 + (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_TOP],
                                    rectWidth,
                                    999);

                var size = new Size(0, 0);
                WriteableBitmap clonedBitmap = bitmap.Clone();

                #region Draw background
                if (hasBackground)
                {
                    using (ImageRender bgRender = new ImageRender(await clonedBitmap.ToRandomAccessStreamAsync()))
                    {
                        double bgHeight = 20;
                        var bgRect = rect;
                        bgRender.BeginDraw();
                        foreach (var renderItem in hot.RenderItems)
                        {
                            if (renderItem.Type == RenderType.Text)
                            {
                                bgRect = new Rect(bgRect.Left, bgRect.Top + bgHeight, bgRect.Width, bgRect.Height);
                                bgHeight += bgRender.MeansureText(renderItem.Content, bgRect, renderItem.TextOption, 1F).Height;
                            }

                            if (renderItem.Type == RenderType.Shape)
                            {
                                bgHeight += 2 + renderItem.ImageOption.Margin.Top + renderItem.ImageOption.Margin.Bottom;
                            }
                        }
                        await bgRender.EndDraw();

                        bgRect = new Rect((double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_LEFT],
                                          (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.POSITION_TOP],
                                          bgWidth,
                                          bgHeight);
                        var b = new WriteableBitmap((int)bgWidth, (int)bgHeight);
                        b.ForEach((x, y) => backcolor);
                        clonedBitmap.Blit(bgRect, b, new Rect(0, 0, (int)bgWidth, (int)bgHeight));
                    }
                }
                #endregion

                using (ImageRender render = new ImageRender(await clonedBitmap.ToRandomAccessStreamAsync()))
                {
                    render.BeginDraw();

                    foreach (var renderItem in hot.RenderItems.OrderBy(i => i.Row))
                    {
                        if (renderItem.Type == RenderType.Text)
                        {
                            rect = new Rect(rect.Left, rect.Top + size.Height, rect.Width, rect.Height);
                            size = render.DrawText(renderItem.Content, rect, renderItem.TextOption, 1F);
                        }

                        if (renderItem.Type == RenderType.Shape)
                        {
                            renderItem.Content += string.Format("|Left:{0}|Top:{1}|Width:{2}|Height:{3}", rect.Left, rect.Top + size.Height, rect.Width, 2);
                            size = new Size(rect.Width, size.Height + 2 + renderItem.ImageOption.Margin.Top + renderItem.ImageOption.Margin.Bottom);
                        }
                    }

                    await render.EndDraw();
                    var image = render.WriteableImage;

                    #region Draw line between summary and title
                    foreach (var renderItem in hot.RenderItems.Where(i => i.Type == RenderType.Shape))
                    {
                        var action = renderItem.Content.StringToDictionary();
                        if (action["Type"] == "Retangle")
                        {
                            image.FillRectangle((int)(action["Left"].StringToDouble() + renderItem.ImageOption.Margin.Left),
                                                (int)(action["Top"].StringToDouble() + renderItem.ImageOption.Margin.Top),
                                                (int)(action["Left"].StringToDouble() + action["Width"].StringToDouble() - renderItem.ImageOption.Margin.Right),
                                                (int)(action["Top"].StringToDouble() + renderItem.ImageOption.Margin.Top + action["Height"].StringToDouble()),
                                                Colors.White);
                        }
                    }
                    #endregion

                    return image;
                }
            }
            catch (Exception ex)
            {
                ex.WriteLog();
                return null;
            }
        }

        public static async Task<byte[]> GetCurrentLockScreen()
        {
            // Get current lockscreen
            byte[] imageBytes = null;
            using (var s = LockScreen.GetImageStream())
            {
                imageBytes = new byte[s.Size];
                await s.ReadAsync(imageBytes.AsBuffer(), (uint)s.Size, InputStreamOptions.None);
            }
            return imageBytes;
        }

        /// <summary>
        ///     Check app run count and show prompt let user to rate and review app in Microsoft Store
        /// </summary>
        /// <returns></returns>
        public static IAsyncAction CheckAndShowRateReviewPromptAsync()
        {
            return AsyncInfo.Run(async token =>
            {
                int initCount = ChameHOTServiceSetting.Instance[ChameHOTServiceSetting.INIT_MAIN_PAGE_COUNT].ToString().StringToInt();
                if (initCount >= 0)
                {
                    if (initCount > 10 && initCount % 10 == 0)
                    {
                        var rateReviewPrompt = new MessageDialog(ResourcesLoader.Loader["ReviewPromptContent"],
                                                                 ResourcesLoader.Loader["ReviewPromptTitle"]);
                        rateReviewPrompt.Commands.Add(new UICommand(ResourcesLoader.Loader["Rate"], null, 2));
                        rateReviewPrompt.Commands.Add(new UICommand(ResourcesLoader.Loader["Never"], null, 1));
                        rateReviewPrompt.Commands.Add(new UICommand(ResourcesLoader.Loader["Later"], null, 0));
                        IUICommand command = await rateReviewPrompt.ShowAsync();

                        if ((int)command.Id == 1) // choose never
                        {
                            ChameHOTServiceSetting.Instance[ChameHOTServiceSetting.INIT_MAIN_PAGE_COUNT] = -1;
                            await AppSettings.SaveSettings(ChameHOTServiceSetting.Instance);
                            return;
                        }

                        if ((int)command.Id == 2) // choose rate it
                        {
                            ChameHOTServiceSetting.Instance[ChameHOTServiceSetting.INIT_MAIN_PAGE_COUNT] = -1;
                            var uri = new Uri("ms-windows-store:REVIEW?PFN=" + Package.Current.Id.FamilyName);
                            await Launcher.LaunchUriAsync(uri);
                            await AppSettings.SaveSettings(ChameHOTServiceSetting.Instance);
                            return;
                        }
                    }

                    ChameHOTServiceSetting.Instance[ChameHOTServiceSetting.INIT_MAIN_PAGE_COUNT] = initCount + 1;
                    await AppSettings.SaveSettings(ChameHOTServiceSetting.Instance);
                }
            });
        }

        /// <summary>
        /// Update tile
        /// </summary>
        /// <param name="hot"></param>
        public static void UpdateTile(HistoryOnToday hot)
        {
            foreach (var item in hot.Items)
            {
                if (!string.IsNullOrEmpty(item.Year) && item.Year.StartsWith("\0")) item.Year = " ";
                if (!string.IsNullOrEmpty(item.Event) && item.Event.StartsWith("\0")) item.Event = " ";

                ITileWide310x150Text04 tileContent = TileContentFactory.CreateTileWide310x150Text04();
                tileContent.TextBodyWrap.Text = " " + item.Year + " " + item.Event;
                ITileSquare150x150Text04 squareContent = TileContentFactory.CreateTileSquare150x150Text04();
                squareContent.TextBodyWrap.Text = " " + item.Year + " " + item.Event;
                tileContent.Square150x150Content = squareContent;
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileContent.CreateNotification());
            }

            ITileWide310x150Text03 tileSummaryContent = TileContentFactory.CreateTileWide310x150Text03();
            tileSummaryContent.TextHeadingWrap.Text = hot.Summary;
            ITileSquare150x150Text04 squareSummaryContent = TileContentFactory.CreateTileSquare150x150Text04();
            squareSummaryContent.TextBodyWrap.Text = hot.Summary;
            tileSummaryContent.Square150x150Content = squareSummaryContent;
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileSummaryContent.CreateNotification());
        }

        #region Private methods

        private static async Task<WriteableBitmap> ResizeBitmap(WriteableBitmap originalBitmap)
        {
            // Get ratio of uniform to fill
            double aspectRatio = (double)(originalBitmap.PixelWidth) / (double)(originalBitmap.PixelHeight);
            double screenWidth = (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_WIDTH];
            double screenHeight = (double)ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_HEIGHT];
            double screenAspectRatio = screenWidth / screenHeight;
            double ratio = aspectRatio < screenAspectRatio ? (double)(screenWidth / originalBitmap.PixelWidth) : (double)(screenHeight / originalBitmap.PixelHeight);

            Size newSize = aspectRatio < screenAspectRatio ? new Size(screenWidth, originalBitmap.PixelHeight * ratio) : new Size(originalBitmap.PixelWidth * ratio, screenHeight);
            var tempBitmap = originalBitmap.Resize((int)newSize.Width, (int)newSize.Height, WriteableBitmapExtensions.Interpolation.NearestNeighbor);
            double offsetY = aspectRatio < screenAspectRatio ? 0.5 * (tempBitmap.PixelHeight - screenHeight) : 0.0;
            double offsetX = aspectRatio < screenAspectRatio ? 0.0 : 0.5 * (tempBitmap.PixelWidth - screenWidth);

            return tempBitmap.Crop(new Rect(offsetX, offsetY, screenWidth, screenHeight));
        }

        #endregion
    }
}
