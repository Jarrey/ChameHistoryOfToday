using NoteOne_Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoteOne_Utility.Converters;
using NoteOne_Utility.Extensions;
using Windows.Data.Xml.Dom;
using ChameHOT_Service.Models;
using System.Xml;
using Windows.Data.Html;
using System.Text.RegularExpressions;
using ChameHOT_Service.Resources;
using ImgHelper = NoteOne_ImageHelper;
using Windows.UI.Text;
using Windows.UI;
using Windows.UI.Xaml;
using NoteOne_ImageHelper;

namespace ChameHOT_Service
{
    public class ChameHOTQueryResult : QueryResult
    {
        public ChameHOTQueryResult(object result, string region)
            : base(result, QueryResultTypes.Single)
        {
            ResponseType = ResponseTypes.Xml;
            Result = new HistoryOnToday();
            Region = region.ToLowerInvariant();

            ParseResponse();
        }

        protected override void ParseResponse()
        {
            try
            {
                base.ParseResponse();

                XmlElement hotElement = null;
                if (ResponseContent != null && ResponseContent is XmlDocument)
                {
                    hotElement = (ResponseContent as XmlDocument).DocumentElement;
                }
                else return;

                var hot = Result as HistoryOnToday;
                var todayXmlElement = hotElement.GetElementsByTagName("entry").LastOrDefault().CheckAndThrow();
                hot.ID = GetXmlValueByNodeName(todayXmlElement, "id");
                hot.Link = GetXmlAttributeByNodeName(todayXmlElement, "link", "href");
                hot.Title = ResourcesLoader.Loader["OnThisDay"];
                hot.Copyright = ResourcesLoader.Loader["CopyrightContent"];

                var summary = GetXmlValueByNodeName(todayXmlElement, "summary");
                hot.Summary = HtmlUtilities.ConvertToText(
                                Regex.Match(summary, _summaryRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline)
                                     .Groups[_contentGroupName].Value).Trim();
                if (string.IsNullOrEmpty(hot.Summary.Trim('\0')))
                    hot.Summary = hot.Title;

                // Format summary for different regions
                FormatWikiDocument(summary, hot);

                // Generate render item models
                GenerateRenderItems(hot);
            }
            catch (Exception ex)
            {
                ex.WriteLog();
            }
        }

        private string GetXmlValueByNodeName(IXmlNode element, string nodeName)
        {
            return element.ChildNodes.FirstOrDefault(e => e.NodeName == nodeName).CheckAndThrow().InnerText;
        }

        private string GetXmlAttributeByNodeName(IXmlNode element, string nodeName, string attributeName)
        {
            return element.ChildNodes.FirstOrDefault(e => e.NodeName == nodeName).CheckAndThrow()
                          .Attributes.FirstOrDefault(a => a.NodeName == attributeName).CheckAndThrow().NodeValue.ToString();
        }

        private void FormatWikiDocument(string html, HistoryOnToday hot)
        {
            var items = Regex.Matches(html, ItemRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match item in items)
            {
                if (_useULTagRegions.Contains(Region))
                {
                    hot.Items.Add(new HistoryItem
                    {
                        Event = HtmlUtilities.ConvertToText(item.Value).Trim()
                    });
                }
                else
                {
                    hot.Items.Add(new HistoryItem
                        {
                            Year = HtmlUtilities.ConvertToText(Regex.Match(item.Value, _itemYearRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline).Value).Trim(),
                            Event = HtmlUtilities.ConvertToText(Regex.Match(item.Value, _itemEventRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline).Value).Trim()
                        });
                }
            }
        }

        private void GenerateRenderItems(HistoryOnToday hot)
        {
            // Add render options for title
            var titleTextOption = new RenderingTextOption()
            {
                FontFamilyName = "Segoe UI",
                FontSize = 30F,
                FontWeight = ImgHelper.FontWeights.Bold,
                Foreground = new ImgHelper.Color(0xff, 0xff, 0xff, 0xff)
            };
            hot.RenderItems.Add(new RenderItemModel { Row = 0, Type = RenderType.Text, TextOption = titleTextOption, Content = hot.Title });

            // Add render options for summary
            var summaryTextOption = new RenderingTextOption()
            {
                FontFamilyName = "Segoe UI",
                FontSize = 24F,
                FontWeight = ImgHelper.FontWeights.Light,
                Foreground = new ImgHelper.Color(0xff, 0xff, 0xff, 0xff),
                WordWrapping = WordWrapping.Wrap
            };
            hot.RenderItems.Add(new RenderItemModel { Row = 1, Type = RenderType.Text, TextOption = summaryTextOption, Content = hot.Summary });

            // Add render options for items and events
            string items = string.Empty;
            foreach (var item in hot.Items)
            {
                items = items + " " + item.Year + " " + item.Event + "\n";
            }
            var itemTextOption = new RenderingTextOption()
            {
                FontFamilyName = "Segoe UI",
                FontSize = 16F,
                Foreground = new ImgHelper.Color(0xff, 0xff, 0xff, 0xff),
                WordWrapping = ImgHelper.WordWrapping.Wrap,
                Margin = new ImgHelper.Thickness(0, 2, 0, 2)
            };
            hot.RenderItems.Add(new RenderItemModel { Row = 3, Type = RenderType.Text, TextOption = itemTextOption, Content = items.TrimEnd() });

            // Add render options for copyright
            var copyrightTextOption = new RenderingTextOption()
            {
                FontFamilyName = "Segoe UI",
                FontSize = 10F,
                Foreground = new ImgHelper.Color(0xff, 0xff, 0xff, 0xff),
                WordWrapping = ImgHelper.WordWrapping.Wrap,
                HorizontalAlignment = ImgHelper.TextHorizontalAlignment.Right
            };
            hot.RenderItems.Add(new RenderItemModel { Row = 4, Type = ImgHelper.RenderType.Text, TextOption = copyrightTextOption, Content = hot.Copyright });

            // Add render options for line
            hot.RenderItems.Add(new RenderItemModel { Row = 2, Type = ImgHelper.RenderType.Shape, Content = "Type:Retangle", ImageOption = new RenderingImageOption() { Margin = new NoteOne_ImageHelper.Thickness(2) } });
        }

        #region Properties

        public string Region { get; private set; }

        #endregion

        #region Regex

        private static string[] _useULTagRegions = new[] { "en", "de" };

        private const string _contentGroupName = "content";

        private const string _summaryRegex = @"<p>(?<content>.*?)</p>";

        private string ItemRegex
        {
            get
            {
                if (_useULTagRegions.Contains(Region))
                {
                    return @"<li>.*?</li>";
                }

                return @"<dt>.*?</dd>";
            }
        }

        private const string _itemYearRegex = @"<dt>.*?</dt>";

        private const string _itemEventRegex = @"<dd>.*?</dd>";

        #endregion
    }
}
