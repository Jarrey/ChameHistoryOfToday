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
    public class ChameHOTRoamingSetting : IAppSettings
    {
        #region For instance

        private static ChameHOTRoamingSetting _instance;

        // For instance
        private ChameHOTRoamingSetting()
        {
            Settings = new ObservableDictionary<string, object>();
            Reset();
        }
        public void Reset()
        {
            Settings[CLIENT_ID] = Guid.NewGuid().ToString();
        }

        #region Setting fields

        public const string CLIENT_ID = "CLIENT_ID";

        #endregion


        public static ChameHOTRoamingSetting Instance
        {
            get
            {
                if (_instance == null) _instance = new ChameHOTRoamingSetting();
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
            get { return "ChameHOT.RoamingSettings.setting"; }
        }

        public SettingType Type
        {
            get { return SettingType.Roaming; }
        }

        public StorageFolder SettingFolder
        {
            get { return ApplicationData.Current.RoamingFolder; }
        }

        public ObservableDictionary<string, object> Settings { get; private set; }

        #endregion
    }
}
