/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration.Data
{
    public class NsiItem
    {
        public string SpravNumber { get; set; }
        public DateTime Created { get; set; }

        public string Code { get; set; }
        public string GUID { get; set; }
        public string Name { get; set; }
        public string Name2 { get; set; }
        public string Values { get; set; }
    }
}
