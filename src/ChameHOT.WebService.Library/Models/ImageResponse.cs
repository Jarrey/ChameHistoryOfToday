using Chame.WebService.Helper;
using Chame.WebService.Helper.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChameHOT.WebService.Library.Models
{
    public class ImageResponse : IChameResponse
    {
        public ImageResponse(Image image, ImageFormat format = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            this.MimeType = ImageUtility.GetMimeType(format ?? image.RawFormat);
            this.Content = new StringContent(Convert.ToBase64String(ImageUtility.ImageToByteArray(image, format)));
            this.Status = status;
        }

        public HttpContent Content { get; private set; }
        public string MimeType { get; private set; }

        public HttpStatusCode Status { get; private set; }
    }
}
