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

        public int[] GetYears()
        {
            List<int> ret = new List<int>();
            int n = 0;

            for (int i = PeriodFrom.Year; i <= PeriodTo.Year; i++)
            {
                ret.Add(i);
                n++;
                if (n > 1000) break;
            }

            return ret.ToArray();
        }

        public int GetStartMonth(int year)
        {
            if (year < PeriodFrom.Year) return 0;
            if (year > PeriodTo.Year) return 0;

            if (year == PeriodFrom.Year) return PeriodFrom.Month;
            else return 1;
        }

        public int GetEndMonth(int year)
        {
            if (year < PeriodFrom.Year) return 0;
            if (year > PeriodTo.Year) return 0;

            if (year == PeriodTo.Year) return PeriodTo.Month;
            else return 12;
        }
    }

    public class ExportWorkingListApiResult : ApiResultBase
    {
        public ExportWorkingListApiResult() { this.Items = new WorkingListItem[0]; }

        public WorkingListItem[] Items { get; set; }        
    }
}
