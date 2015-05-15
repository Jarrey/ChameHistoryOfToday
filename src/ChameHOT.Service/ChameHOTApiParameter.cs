using NoteOne_Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChameHOT_Service
{
    public class ChameHOTApiParameter : ApiParameter
    {
        public ChameHOTApiParameter()
        {
            Parameters.Add(0, () => Region);
        }

        public string Region { get; set; }
    }
}
