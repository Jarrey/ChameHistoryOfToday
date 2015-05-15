using ChameHOT_Service.Resources;
using NoteOne_Core;
using NoteOne_Core.Common;
using NoteOne_Core.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChameHOT_Service.Models
{
    [DataContract]
    public class ChameHOTServiceChannelModel : ServiceChannelModel
    {
        /// <summary>
        ///     Is show overlay in page, defalt is True
        /// </summary>
        [DataMember]
        private bool _showOverlay = true;

        public ChameHOTServiceChannelModel(ServiceChannel channel) : base(channel)
        {
            Index = 30;
            Title = ""; // ResourcesLoader.Loader["ServiceChannelTitle"];
            SubTitle = ""; // ResourcesLoader.Loader["ServiceChannelSubTitle"];
            Logo = new Collection<BindableImage>
                {
                    new BindableImage
                        {
                            ThumbnailImageUrl = @"ms-appx:///Assets/ServiceChannelLogo//ChannelLogo4x4.png",
                            IsThumbnailImageDownloading = false
                        }
                };
            GroupID = ServiceChannelGroupID.OnlinePictures;
            // PrimaryViewType = typeof(AstronomyPictureServiceChannelPage);
        }

        public bool ShowOverlay
        {
            get { return _showOverlay; }
            set { SetProperty(ref _showOverlay, value); }
        }

    }
}
