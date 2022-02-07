/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class ExportPkzRes : ApiResultBase
    {
        public string HouseGUID { get; set; }
        public List<PkzEntry> Entries { get; set; }

        public ExportPkzRes()
        {
            Entries = new List<PkzEntry>(100);
        }
    }
}
