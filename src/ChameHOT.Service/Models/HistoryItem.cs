#if WEB_SERVICE
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace ChameHOT.WebService.Library.Models
{
#else
using NoteOne_Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Html;

namespace ChameHOT_Service.Models
{
#endif

#if !WEB_SERVICE
    [DataContract]
#endif
    public class HistoryItem
#if !WEB_SERVICE 
        : ModelBase
#endif
    {
        private string _year;
        private string _event;

#if !WEB_SERVICE
        [DataMember]
#endif
        public string Year
        {
            get { return _year; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _year, value);
#else
                _year = value;
#endif
            }
        }

#if !WEB_SERVICE
        [DataMember]
#endif
        public string Event
        {
            get { return _event; }
            set
            {
#if !WEB_SERVICE
                SetProperty(ref _event, value);
#else
                _event = value;
#endif
            }
        }
    }
}
