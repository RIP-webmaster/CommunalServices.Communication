/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration.API
{
    public class GetAppealsRes : ApiResultBase
    {
        public int RequestState { get; set; }        
        public List<GISGKHIntegration.Data.Appeal> Entries { get; set; }

        public GetAppealsRes()
        {
            Entries = new List<GISGKHIntegration.Data.Appeal>(100);
        }
    }
}
