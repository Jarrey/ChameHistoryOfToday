using NoteOne_Utility;
using NoteOne_Utility.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ChameHOT_Service
{
    public class ChameHOTServiceSetting : IAppSettings
    {
        #region For instance

        private static ChameHOTServiceSetting _instance;

        // For instance
        private ChameHOTServiceSetting()
        {
            Settings = new ObservableDictionary<string, object>();
            Reset();
        }

        public void Reset()
        {
            Settings[UPGRADE_KEY] = "0F3B18E2-4816-4C16-9876-464B843382F7";

            Settings[INIT_MAIN_PAGE_COUNT] = 0;

            Settings[POSITION_LEFT] = 800d;
            Settings[POSITION_TOP] = 100d;
            Settings[BACKCOLOR_ON] = true;
            Settings[BACKCOLOR] = "#66222222";
            Settings[SCREEN_WIDTH] = 1280d;
            Settings[SCREEN_HEIGHT] = 800d;

            // The count of cache keys for image in local and roaming folder
            Settings[CACHE_COUNT] = 20;
        }

        #region Setting fields

        /// <summary>
        ///     A key used to cleanup app data after upgrading app
        /// </summary>
        public const string UPGRADE_KEY = "UPGRADE_KEY";

        /// <summary>
        ///     Count main page init times, if it can mod 10, show review and rate prompt
        ///     -1 means never show the prompt
        /// </summary>
        public const string INIT_MAIN_PAGE_COUNT = "INIT_MAIN_PAGE_COUNT";

        public const string POSITION_LEFT = "POSITION_LEFT";
        public const string POSITION_TOP = "POSITION_TOP";
        public const string BACKCOLOR_ON = "BACKCOLOR_ON";
        public const string BACKCOLOR = "BACKCOLOR";
        public const string SCREEN_WIDTH = "SCREEN_WIDTH";
        public const string SCREEN_HEIGHT = "SCREEN_HEIGHT";
        public const string CACHE_COUNT = "CACHE_COUNT";

        #endregion

        public static ChameHOTServiceSetting Instance
        {
            get
            {
                if (_instance == null) _instance = new ChameHOTServiceSetting();
                return _instance;
            }
        }

        public object this[string keyName]
        {
            get
            {
                if (Settings.ContainsKey(keyName)) return Settings[keyName];
                else
                    return null;
            }
            set { if (Settings.ContainsKey(keyName)) Settings[keyName] = value; }
        }

        public string SettingFileName
        {
            get { return "ChameHOT.ServiceSettings.setting"; }
        }

        public SettingType Type
        {
            get { return SettingType.Local; }
        }

        public StorageFolder SettingFolder
        {
            get { return ApplicationData.Current.LocalFolder; }
        }

        public ObservableDictionary<string, object> Settings { get; private set; }

        #endregion
    }
}
