#if WEB_SERVICE
using ChameHOT.WebService.Helpers.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace ChameHOT.WebService.Library.Models
{
#else
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
#endif

#if !WEB_SERVICE
    [DataContract]
#endif
    public class HistoryOnToday
#if !WEB_SERVICE 
        : ModelBase
#endif
    {
        private string _id;
        private string _summary;
        private string _link;
        private string _title;
        private string _copyright;
        private List<HistoryItem> _items;
        private List<RenderItemModel> _renderItems;

        public HistoryOnToday()
        {
            Items = new List<HistoryItem>();
            RenderItems = new List<RenderItemModel>();
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public string ID
        {
            get { return _id; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _id, value);
#else
                _id = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public string Summary
        {
            get { return _summary; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _summary, value);
#else
                _summary = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public string Link
        {
            get { return _link; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _link, value);
#else
                _link = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public string Title
        {
            get { return _title; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _title, value);
#else
                _title = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public List<HistoryItem> Items
        {
            get { return _items; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _items, value);
#else
                _items = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public string Copyright
        {
            get { return _copyright; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _copyright, value);
#else
                _copyright = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public List<RenderItemModel> RenderItems
        {
            get { return _renderItems; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _renderItems, value);
#else
                _renderItems = value;
#endif
            }
        }
    }
}
