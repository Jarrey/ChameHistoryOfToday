using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using NoteOne_Utility.Extensions;
using Newtonsoft.Json;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Helpers;

namespace ChameHOT_Service
{
    public class ChameHOTCacheKeys
    {
        private static readonly AsyncLock KEYS_FILE_WRITE_ASYNC_LOCKER = new AsyncLock();
        private static string KeyFileName = "keys.json";
        private static List<string> _localKeys = new List<string>();
        private static List<string> _roamingKeys = new List<string>();

        public async static Task AddKey(string key)
        {
            _localKeys = await InternalGetKeys(ApplicationData.Current.LocalFolder, _localKeys);
            _roamingKeys = await InternalGetKeys(ApplicationData.Current.RoamingFolder, _roamingKeys);
            if (!_localKeys.Contains(key))
            {
                _localKeys.Add(key);
            }

            if (!_roamingKeys.Contains(key))
            {
                _roamingKeys.Add(key);
            }

            RemoveOldKeys();

            await InternalSaveKeys(ApplicationData.Current.LocalFolder, _localKeys);
            await InternalSaveKeys(ApplicationData.Current.RoamingFolder, _roamingKeys);
        }

        public async static Task<bool> IsContainKey(string key)
        {
            bool result = true;
            _localKeys = await InternalGetKeys(ApplicationData.Current.LocalFolder, _localKeys);
            _roamingKeys = await InternalGetKeys(ApplicationData.Current.RoamingFolder, _roamingKeys);
            if (!_localKeys.Contains(key))
            {
                _localKeys.Add(key);
                result = false;
            }

            if (!_roamingKeys.Contains(key))
            {
                _roamingKeys.Add(key);
                result = false;
            }

            return result;
        }

        private static void RemoveOldKeys()
        {
            // Remove old keys
            var cacheCount = ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.CACHE_COUNT].ToString().StringToInt();
            if (_localKeys.Count > cacheCount)
            {
                _localKeys.RemoveRange(0, _localKeys.Count - cacheCount);
            }

            if (_roamingKeys.Count > cacheCount)
            {
                _roamingKeys.RemoveRange(0, _roamingKeys.Count - cacheCount);
            }
        }

        private async static Task InternalSaveKeys(StorageFolder folder, List<string> keys)
        {
            using (AsyncLock.Releaser releaser = await KEYS_FILE_WRITE_ASYNC_LOCKER.LockAsync())
            {
                StorageFile file = null;
                var k = await JsonConvert.SerializeObjectAsync(keys);
                if (await folder.CheckFileExisted(KeyFileName) == false)
                {
                    file = await folder.CreateFileAsync(KeyFileName, CreationCollisionOption.ReplaceExisting);
                }
                else
                {
                    file = await folder.GetFileAsync(KeyFileName);
                }

                await FileIO.WriteTextAsync(file, k);
            }
        }

        private async static Task<List<String>> InternalGetKeys(StorageFolder folder, List<string> keys)
        {
            using (AsyncLock.Releaser releaser = await KEYS_FILE_WRITE_ASYNC_LOCKER.LockAsync())
            {
                if (await folder.CheckFileExisted(KeyFileName) == false)
                {
                    var k = await JsonConvert.SerializeObjectAsync(keys);
                    await FileIO.WriteTextAsync(await folder.CreateFileAsync(KeyFileName, CreationCollisionOption.ReplaceExisting), k);
                }
            }

            // Read the cahe keys
            return await JsonConvert.DeserializeObjectAsync<List<string>>(await FileIO.ReadTextAsync(await folder.GetFileAsync(KeyFileName)));
        }
    }
}
