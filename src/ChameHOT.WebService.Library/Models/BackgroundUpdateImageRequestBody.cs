using Chame.WebService.Helper;
using Chame.WebService.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChameHOT.WebService.Library.Models
{
    public class BackgroundUpdateImageRequestBody : IChameRequest
    {
        public string ClientId { get; set; }

        public HistoryOnToday HOT { get; set; }

        public string ImageBase64 { get; set; }

        public SizeD ScreenSize { get; set; }

        public System.Drawing.PointF Position { get; set; }

        public bool HasBackground { get; set; }

        public string BackgroundColor { get; set; }
    }
}