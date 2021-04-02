using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Xades.GIS
{
    public class XadesInfo
    {
        public string Thumbprint { get; set; }
        public string RawPK { get; set; }
        public DateTime SigningDateTimeUTC { get; set; }
        public int TimeZoneOffsetMinutes { get; set; }
    }
}
