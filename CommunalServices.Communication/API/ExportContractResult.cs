/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration.API
{
    public class ExportContractRes : ApiResultBase
    {
        public GKH.exportCAChResultType RawData { get; set; }
    }
}
