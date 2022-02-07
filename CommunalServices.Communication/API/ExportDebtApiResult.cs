//Svitkin 2021
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.Data;

namespace GISGKHIntegration
{
    public class ExportDebtApiResult:ApiResultBase
    {
        public ExportDebtApiResult()
        {
            this.NextPageGuid = string.Empty;
        }
                
        public DebtRequest[] Requests { get; set; }
        public string NextPageGuid { get; set; }
    }
}
