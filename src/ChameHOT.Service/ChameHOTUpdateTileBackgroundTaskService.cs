using ChameHOT.BackgroundTask;
using NoteOne_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using NoteOne_Utility;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using NoteOne_Utility.Helpers;
using Windows.UI.Notifications;
using NoteOne_Core.Notifications;
using NoteOne_Core.Notifications.TileContent;

namespace ChameHOT_Service
{
    public class ChameHOTUpdateTileBackgroundTaskService : BackgroundTaskService
    {
        public ChameHOTUpdateTileBackgroundTaskService(Service service, XmlElement configXml)
            : base(service, configXml, UpdateTileBackgroundTask.BackgroundTaskSettingFileName)
        {
            DoAsync = Task_Run;
        }

        #region Properties

        public uint TimeTriggerTime { get; private set; }

        #endregion

        protected override void Initialize(XmlElement configXml)
        {
            base.Initialize(configXml);

            try
            {
                string[] times = configXml.GetAttribute("TimeTriggerTimes").Check("").StringToArray();
                foreach (string time in times)
                {
                    string[] tc = time.StringToArray(':');
                    if (tc.Length > 1)
                        TimeTriggerTime = tc[1].StringToUInt();
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
                    var hot = await chameHOTQueryService.QueryDataAsync();

                    ChameHOTServiceHelper.UpdateTile(hot);
                }
            }
            catch (Exception ex)
            {
                ex.WriteLog();
            }
        }
    }
}
