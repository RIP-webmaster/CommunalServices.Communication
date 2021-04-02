/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class MDevice
    {
        public string idf { get; set; }
        public string gisgkh_num { get; set; }
        public string regnum { get; set; }

        public string DeviceGUID { get; set; }
        public string DeviceVGUID { get; set; }
        public string AccountGUID { get; set; }
        public string PremisesGUID { get; set; }
        public string ResourceGUID { get; set; }

        public bool IsElectric { get; set; }

    }
}
