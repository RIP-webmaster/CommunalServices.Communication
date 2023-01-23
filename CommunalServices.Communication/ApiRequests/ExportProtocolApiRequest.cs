/* Communal services system integration 
 * Copyright (c) 2023,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.API;
using CommunalServices.Communication.Data;
using GISGKHIntegration;
using GKH;

namespace CommunalServices.Communication.ApiRequests
{
    /// <summary>
    /// Экспорт информации о протоколе
    /// </summary>
    public class ExportProtocolApiRequest : ApiRequestBase
    {
        public ExportProtocolApiRequest(string orgPPAGUID, int k_post, string houseGuid)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.HouseGuid = houseGuid;
        }

        public string HouseGuid { get; set; }
        
        public override ApiResultBase Send()
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync");
                ApiResult apires = new ApiResult();

                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса

                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                hdr.Item = this.OrgPpaGuid;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                var request = new exportVotingProtocolRequest();
                request.Id = "signed-data-container";
                request.version = "13.1.0.4";
                request.Items = new object[2];
                request.ItemsElementName = new ItemsChoiceType35[2];
                request.Items[0] = this.HouseGuid;
                request.Items[1] = exportVotingProtocolRequestStatusVersionProtocol.Posted;
                request.ItemsElementName[0] = ItemsChoiceType35.FIASHouseGuid;
                request.ItemsElementName[1] = ItemsChoiceType35.StatusVersionProtocol;

                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var res = proxy.exportVotingProtocol(hdr, request, out ack);

                    long t2 = Environment.TickCount;
                    apires.in_xml = GisAPI.LastRequest;
                    apires.out_xml = GisAPI.LastResponce;
                    apires.query_duration = (t2 - t1) / 1000.0M;
                    apires.date_query = DateTime.Now;

                    StringBuilder sb = new StringBuilder(300);

                    if (res == null) { apires.text = ("service returned null"); return apires; }

                    var resAck = ack.Ack;
                    apires.messageGUID = resAck.MessageGUID;
                    this.MessageGuid = resAck.MessageGUID;
                    sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                    sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                    sb.AppendLine("TransportGUID: " + apires.transportGUID);

                    sb.AppendLine("Дата и время запроса: " + apires.date_query);
                    sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                    apires.text = sb.ToString();

                    return apires;
                }
                catch (Exception exc)
                {
                    ApiResultBase.InitExceptionResult(apires, "ExportProtocol", exc);
                    return apires;
                }
                finally
                {
                    try { proxy.Close(); }
                    catch (Exception) { }
                }

            }//end lock
        }

        public override ApiResultBase CheckState()
        {
            ApiResultBase ret = ExportContractApiRequest.ExportContract_Check(this.MessageGuid, this.OrgPpaGuid);
            return ret;
        }
    }
}

