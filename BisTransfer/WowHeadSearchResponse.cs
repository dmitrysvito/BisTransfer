using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTransfer
{
    internal class WowHeadSearchResponse
    {
        public string search { get; set; }
        public WowHeadResult[] results { get; set; }
    }
}
