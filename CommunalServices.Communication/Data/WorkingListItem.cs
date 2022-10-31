/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using GISGKHIntegration;

namespace CommunalServices.Communication.Data
{
    public class WorkingListItem
    {
        public string WorkListGUID { get; set; }
        public string FIASHouseGuid { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public string ContractGUID { get; set; }
        public string Status { get; set; }

        public string WorkListItemGUID { get; set; }
        public int Count { get; set; }
    }

    public class ExportWorkingListApiResult : ApiResultBase
    {
        public ExportWorkingListApiResult() { this.Items = new WorkingListItem[0]; }

        public WorkingListItem[] Items { get; set; }        
    }
}
