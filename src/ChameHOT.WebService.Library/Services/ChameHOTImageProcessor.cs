using Chame.WebService.Helper;
using Chame.WebService.Helper.Services;
using ChameHOT.WebService.Helpers.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ChameHOT.WebService.Library.Services
{
    public class ChameHOTImageProcessor : IDisposable
    {
        private Graphics graphics = null;
        private readonly Image image;

        #region Constructors

        public ChameHOTImageProcessor(Image bitmap)
        {
            this.image = bitmap;
            this.graphics = Graphics.FromImage(this.image);
        }

        public void Dispose()
        {
            if (graphics != null)
                graphics.Dispose();
            graphics = null;
        }

        #endregion

        internal void ProcessImage(PointF startPosition, IEnumerable<RenderItemModel> renderModels, string backgroundColor, bool background = false)
        {
            float rectWidth = 382;
            var rect = new RectangleF(9 + startPosition.X, 9 + startPosition.Y, rectWidth, 999);
            var size = new SizeF(0, 0);
            float top = startPosition.Y;
            var bgcolor = new SolidBrush(ColorTranslator.FromHtml(backgroundColor));

            // Draw bg function
            var drawBackground = new Action<float, float>((t, h) => graphics.FillRectangle(bgcolor, startPosition.X, t, 400, h));

            drawBackground(top, 20);
            top += 20;
            foreach (var renderItem in renderModels.OrderBy(i => i.Row))
            {
                if (renderItem.Type == RenderType.Text)
                {
                    if (background)
                    {
                        // Draw background
                        var height = GraphicsRenderHelper.MeasureText(graphics, renderItem, 1F, size, rect).Height;
                        drawBackground(top, height);
                    }

                    GraphicsRenderHelper.RenderText(graphics, renderItem, 1F, ref size, ref rect);
                }

                if (renderItem.Type == RenderType.Shape)
                {
                    var action = renderItem.Content.StringToDictionary();
                    if (action["Type"] == "Retangle")
                    {
                        var brush = new SolidBrush(System.Drawing.Color.White);
                        var height = (float)(2F + renderItem.ImageOption.Margin.Top + renderItem.ImageOption.Margin.Bottom);

                        if (background)
                        {
                            // Draw background
                            drawBackground(top, height);
                        }

                        rect = new RectangleF(rect.Left, rect.Top + size.Height, rect.Width, rect.Height);
                        graphics.FillRectangle(brush,
                                               (float)(rect.Left + renderItem.ImageOption.Margin.Left),
                                               (float)(rect.Top + renderItem.ImageOption.Margin.Top),
                                               (float)(rect.Width - renderItem.ImageOption.Margin.Right),
                                               2F);
                        size = new SizeF(rect.Width, height);
                    }
                }

                top += size.Height;
            }
        }
    }
}
