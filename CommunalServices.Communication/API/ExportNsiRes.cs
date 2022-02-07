/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class ExportNsiRes : ApiResultBase
    {
        public List<Data.NsiItem> NsiItems { get; set; }

        public ExportNsiRes()
        {
            NsiItems = new List<Data.NsiItem>(50);
        }

    }
}
