using Chame.WebService.Helper.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChameHOT.WebService.Library.Models
{
    public class JsonResponse : IChameResponse
    {
        public JsonResponse(object obj, HttpStatusCode status = HttpStatusCode.OK)
        {
            this.Content = new StringContent(JsonConvert.SerializeObject(obj));
            this.Status = status;
        }

        public HttpContent Content { get; private set; }

        public string MimeType
        {
            get { return "application/json"; }
        }

        public HttpStatusCode Status { get; private set; }
    }
}
