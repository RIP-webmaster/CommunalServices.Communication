﻿/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace GISGKHIntegration
{
    public class PkzEntry
    {
        public PkzEntry()
        {
            this.HouseGUID = string.Empty;
        }

        public string DeviceGUID { get; set; }
        public string ResourceGUID { get; set; }
        public string HouseGUID { get; set; }

        public int n_pkz { get; set; }
        public decimal Value {get;set;}

        public string Source { get; set; }
        public DateTime DatePkz { get; set; }
    }
}
