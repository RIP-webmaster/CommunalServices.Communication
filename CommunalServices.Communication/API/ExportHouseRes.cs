/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class ExportHouseRes :ApiResultBase
    {
        public string HouseGUID { get; set; }
        public string UniqueNumber { get; set; }
        public DateTime ModificationDate { get; set; }

        public List<Data.Premises> LivingPremises { get; set; }
        public List<Data.Premises> NonLivingPremises { get; set; }
    }
}
