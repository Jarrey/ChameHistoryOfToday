using NoteOne_Core.Common;
using NoteOne_ImageHelper.ImageSecurity;
using System.Runtime.Serialization;
using Windows.Foundation;

namespace ChameHOT_Service.Models
{
    [DataContract]
    public class BackgroundTaskRequestModel
    {
        [DataMember]
        public string ClientId { get; set; }
        [DataMember]
        public HistoryOnToday HOT { get; set; }
        [DataMember]
        public string ImageBase64 { get; set; }
        [DataMember]
        public Size ScreenSize { get; set; }
        [DataMember]
        public Point Position { get; set; }
        [DataMember]
        public bool HasBackground { get; set; }
        [DataMember]
        public string BackgroundColor { get; set; }
    }
}
