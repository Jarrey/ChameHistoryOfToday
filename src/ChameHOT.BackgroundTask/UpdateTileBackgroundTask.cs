using NoteOne_Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using NoteOne_Utility;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using NoteOne_Utility.Helpers;
using NoteOne_Core;

namespace ChameHOT.BackgroundTask
{
    public sealed class UpdateTileBackgroundTask : IBackgroundTask
    {
        public static string BackgroundTaskSettingFileName { get { return "ChameHOT_TileBackgroundTask.setting"; } }

        private BackgroundTaskDeferral _deferral;

        /// <summary>
        ///     Performs the work of a background task. The system calls this method when the associated background task has been triggered.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task. The system creates this instance when the task has been triggered to run.</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            StorageFile settingFile = await ApplicationData.Current.LocalFolder.GetFileAsync(BackgroundTaskSettingFileName);
            XmlDocument xmlSetting = await XmlDocument.LoadFromFileAsync(settingFile);

            ServiceTypes serviceType;
            IXmlNode selectSingleNode =
                xmlSetting.SelectSingleNode("/BackgroundTaskServices/BackgroundTaskService/@ServiceType");
            if (selectSingleNode != null)
            {
                string serviceTypeValue = selectSingleNode.InnerText;
                IXmlNode singleNode = xmlSetting.SelectSingleNode("/BackgroundTaskServices/BackgroundTaskService/@Parameter");
                if (singleNode != null)
                {
                    string parameter = singleNode.InnerText;
                    if (Enum.TryParse(serviceTypeValue, out serviceType))
                    {
                        if (serviceType == ServiceTypes.Online)
                            RunService(xmlSetting, parameter);
                        else if (serviceType == ServiceTypes.Local)
                            RunService(xmlSetting, parameter);
                    }
                }
            }
        }

        /// <summary>
        ///     Runs the service.
        /// </summary>
        /// <param name="xmlSetting">The XML setting.</param>
        /// <param name="parameter">The parameter.</param>
        private async void RunService(XmlDocument xmlSetting, string parameter)
        {
            IXmlNode selectSingleNode = xmlSetting.SelectSingleNode("/BackgroundTaskServices/BackgroundTaskService");
            if (selectSingleNode != null)
            {
                string serviceChannelConfigXml = selectSingleNode.InnerText;
                var xmlserviceChannelConfigXmlDoc = new XmlDocument();
                xmlserviceChannelConfigXmlDoc.LoadXml(serviceChannelConfigXml);
                Activator.CreateInstance(
                    xmlserviceChannelConfigXmlDoc.DocumentElement.GetAttribute("Type").CheckAndThrow().GenerateType(),
                    xmlserviceChannelConfigXmlDoc.DocumentElement);
            }

            IXmlNode singleNode = xmlSetting.SelectSingleNode("/BackgroundTaskServices/BackgroundTaskService/@ServiceChannel");
            if (singleNode != null)
            {
                var serviceChannel = (ServiceChannel)ServiceChannelManager.CurrentServiceChannelManager[new Guid(singleNode.InnerText)];
                IXmlNode xmlNode = xmlSetting.SelectSingleNode("/BackgroundTaskServices/BackgroundTaskService/@Service");
                if (xmlNode != null)
                {
                    var service = serviceChannel[new Guid(xmlNode.InnerText)] as Service;

                    // Parse parameter
                    Dictionary<string, string> parameters = parameter.StringToDictionary();

                    if (service != null) await service.BackgroundTaskService.DoAsync(parameters);
                }
            }

            _deferral.Complete();
        }
    }
}
