using NoteOne_Core.Common;
using NoteOne_ImageHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChameHOT_Service.Models
{
    [DataContract]
    internal class BackgroundRequestModel : ModelBase
    {
        public BackgroundRequestModel(string id, string baseImage, HistoryOnToday hot, List<RenderItemModel> renderItems)
        {
            this.ClientId = id;
            this.ImageBase64 = baseImage;
            this.HOT = hot;
            this.RenderItems = renderItems;
        }

        [DataMember]
        public string ClientId { get; set; }
        [DataMember]
        public string ImageBase64 { get; set; }
        [DataMember]
        public HistoryOnToday HOT { get; set; }

        [DataMember]
        public List<RenderItemModel> RenderItems { get; set; }
    }
}
