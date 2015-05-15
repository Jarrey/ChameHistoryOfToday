using Chame.WebService.Helper.Services;
using ChameHOT.WebService.Library.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using Chame.WebService.Helper.Models;
using Chame.WebService.Helper;
using System.Net.Http;
using System.Web;
using System.Net;
using log4net;

namespace ChameHOT.WebService.Library.Services
{
    public class ChameHOTImageService : IChameService
    {
        private readonly ILog logger;

        public ChameHOTImageService(ILog logger)
        {
            this.logger = logger;
        }

        public void Dispose()
        {
            // Not implement, to clean up instance
        }

        #region Properties

        public string ServiceName
        {
            get { return "ChameHOT.Service"; }
        }

        #endregion

        public async Task<IChameResponse> ProcessImage(string request)
        {
            // Deserialize request to request object
            var reqeustBody = await JsonConvert.DeserializeObjectAsync<BackgroundUpdateImageRequestBody>(request);

            logger.InfoFormat("Client id: {0}", reqeustBody.ClientId);

            // Check and select user chached lockscreen image
            using (var originalImage = ImageUtility.Base64ToImage(reqeustBody.ImageBase64))
            {
                using (var bitmap = ImageUtility.GetFitImage(originalImage, new SizeF((float)reqeustBody.ScreenSize.Width, (float)reqeustBody.ScreenSize.Height)))
                {
                    using (var processor = new ChameHOTImageProcessor(bitmap))
                    {
                        // Process Image
                        logger.InfoFormat("Processing image @ position: {0}", reqeustBody.Position);
                        processor.ProcessImage(reqeustBody.Position, reqeustBody.HOT.RenderItems, reqeustBody.BackgroundColor, reqeustBody.HasBackground);
                    }

                    logger.InfoFormat("{0} Processed!!", reqeustBody.ClientId);
                    return new ImageResponse(bitmap/*, originalImage.RawFormat*/);
                }
            }
        }

        public async Task<IChameResponse> GetCache()
        {
            return new JsonResponse("Not support cache!");
        }

        public async Task<IChameResponse> CleanCache(string id = null)
        {
            return new JsonResponse("Not support cache!");
        }

        #region Helper Methods

        private async Task<BackgroundUpdateImageRequestBody> DeserializeRequest(string request)
        {
            var setting = new JsonSerializerSettings() { FloatParseHandling = FloatParseHandling.Decimal };
            return await JsonConvert.DeserializeObjectAsync<BackgroundUpdateImageRequestBody>(request, setting);
        }

        #endregion
    }
}
