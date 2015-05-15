using ChameHOT_Service.Models;
using NoteOne_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;

namespace ChameHOT_Service
{
    public class ChameHOTServiceChannel : ServiceChannel
    {
        /// <summary>
        ///     ID is [Guid("B90BE798-8A61-446E-AB0B-E57D4569CAC1")]
        /// </summary>
        public ChameHOTServiceChannel(XmlElement configXml) : base(configXml)
        {
        }
        
        protected override void InitializeServiceChannel(XmlElement configXml)
        {
            base.InitializeServiceChannel(configXml);

            if (ID.CompareTo(new Guid("B90BE798-8A61-446E-AB0B-E57D4569CAC1")) != 0)
                throw new InvalidOperationException("The ServiceChannel ID is incorrect.");

            Model = new ChameHOTServiceChannelModel(this);
        }

        public override async void InitializeLogo()
        {
        }
    }
}
