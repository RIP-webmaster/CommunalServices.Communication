/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class ExportAccountsRes : ApiResultBase
    {
        public string HouseGUID { get; set; }
        public int RequestState { get; set; }

        public List<Data.Account> Accounts { get; set; }

        public ExportAccountsRes()
        {
            Accounts = new List<Data.Account>(200);
        }
        
    }
}
