//Svitkin 2021
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.Data;

namespace GISGKHIntegration
{
    public class ExportDebtApiResult:ApiResultBase
    {
        public int RequestState { get; set; }
        public DebtRequest[] Requests { get; set; }
    }
}
