/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.API;
using CommunalServices.Communication.Data;
using GISGKHIntegration;
using GisgkhServices.Debt;

namespace CommunalServices.Communication.ApiRequests
{
    /// <summary>
    /// Отправка ответов на запросы о задолженности (поддерживает размещение информации о наличии задолженности)
    /// </summary>
    public class ImportDebtInfoApiRequest : ApiRequestBase
    {
        public ImportDebtInfoApiRequest(string orgPPAGUID, DebtInfo[] responses, string executorGuid, int k_post)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.Responses = responses;
            this.ExecutorGuid = executorGuid;
        }

        public DebtInfo[] Responses { get; set; }

        public string ExecutorGuid { get; set; }

        public override ApiResultBase Send()
        {
            ApiResultBase ret = DebtAPI.ImportDebtInfo_Begin(this.OrgPpaGuid, this.Responses, this.ExecutorGuid);
            this.MessageGuid = ret.messageGUID;
            return ret;
        }

        public override ApiResultBase CheckState()
        {
            ApiResultBase ret = DebtAPI.ExportDebtRequests_Check(this.MessageGuid, this.OrgPpaGuid, this.KPost);
            return ret;
        }
    }
}
