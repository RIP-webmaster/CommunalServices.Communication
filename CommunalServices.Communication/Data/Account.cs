/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration.Data
{
    public class Account
    {
        public string LS { get; set; }
        public string ELS { get; set; }
        public string GKUID { get; set; }
        public string AccountGUID { get; set; }
        public string PremisesGUID { get; set; }
        public string ReasonType { get; set; }
        public string ReasonGUID { get; set; }
        public string OrgPPAGUID { get; set; }
    }
}
